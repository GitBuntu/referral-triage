using System;
using System.Diagnostics;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Models;
using ReferralTriageApp.Infrastructure;

namespace ReferralTriageApp.Services;

public class TriageProcessingService : ITriageProcessingService
{
    // Quality gate constants
    private const string DEFAULT_CONFIDENCE_THRESHOLD_KEY = "ReferralTriageApp:ConfidenceThreshold";
    private const double DEFAULT_CONFIDENCE_THRESHOLD = 0.90;
    private const int MAX_RETRIES = 2;

    private readonly BlobServiceClient _blobServiceClient;
    private readonly ReferralTriageContext _dbContext;
    private readonly IDocumentExtractionService _documentExtractionService;
    private readonly ITriageClassificationService _triageClassificationService;
    private readonly IValidationService _validationService;
    private readonly IDeadLetterService _deadLetterService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TriageProcessingService> _logger;

    public TriageProcessingService(
        BlobServiceClient blobServiceClient,
        ReferralTriageContext dbContext,
        IDocumentExtractionService documentExtractionService,
        ITriageClassificationService triageClassificationService,
        IValidationService validationService,
        IDeadLetterService deadLetterService,
        IConfiguration configuration,
        ILogger<TriageProcessingService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _dbContext = dbContext;
        _documentExtractionService = documentExtractionService;
        _triageClassificationService = triageClassificationService;
        _validationService = validationService;
        _deadLetterService = deadLetterService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ProcessTriageAsync(string referralId, string documentFormat, string blobPath)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("TRIAGE_PIPELINE_START: ReferralId={ReferralId}, DocumentFormat={DocumentFormat}, BlobPath={BlobPath}",
                referralId, documentFormat, blobPath);

            // Extract text from document (with retry logic)
            var extractedText = await RetryHelper.RetryAsync(
                async () => await _documentExtractionService.ExtractTextFromDocumentAsync(blobPath, documentFormat),
                maxRetries: MAX_RETRIES,
                logger: _logger,
                operationName: "document_extraction");

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("TRIAGE_EXTRACTION_FAILED: ReferralId={ReferralId}, ExtractedText is empty/null after {MaxRetries} retries",
                    referralId, MAX_RETRIES);
                await _deadLetterService.EmitToDeadLetterAsync(
                    referralId,
                    "document_extraction_failed",
                    "Extracted text is empty or whitespace",
                    retryCount: MAX_RETRIES);
                await UpdateReferralStatusAsync(referralId, "failed");
                return;
            }

            _logger.LogInformation("Extraction succeeded for ReferralId={ReferralId}, TextLength={TextLength}", referralId, extractedText.Length);

            // Classify with AI model (with retry logic)
            var triageRequest = new TriageRequest
            {
                ReferralId = referralId,
                DocumentFormat = documentFormat,
                ExtractedText = extractedText
            };

            var triageResponse = await RetryHelper.RetryAsync(
                async () => await _triageClassificationService.ClassifyReferralAsync(triageRequest),
                maxRetries: MAX_RETRIES,
                logger: _logger,
                operationName: "document_classification");

            if (triageResponse == null)
            {
                _logger.LogWarning("TRIAGE_CLASSIFICATION_FAILED: ReferralId={ReferralId}, Response is null after {MaxRetries} retries",
                    referralId, MAX_RETRIES);
                await _deadLetterService.EmitToDeadLetterAsync(
                    referralId,
                    "classification_failed",
                    "AI classification returned null after max retries",
                    retryCount: MAX_RETRIES);
                await UpdateReferralStatusAsync(referralId, "failed");
                return;
            }

            _logger.LogInformation("Classification succeeded for ReferralId={ReferralId}, Specialty={Specialty}, Urgency={Urgency}, ConfidenceScore={ConfidenceScore}",
                referralId, triageResponse.Specialty, triageResponse.Urgency, triageResponse.ConfidenceScore);

            // Validate triage record
            var triageRecord = new Models.TriageRecord
            {
                Id = referralId,
                ReferralId = referralId,
                Specialty = triageResponse.Specialty,
                Urgency = triageResponse.Urgency,
                ExtractedFields = triageResponse.ExtractedFields,
                ClinicalSummary = triageResponse.ClinicalSummary,
                OriginalText = extractedText,
                TriagedAt = DateTime.UtcNow,
                ConfidenceScore = triageResponse.ConfidenceScore
            };

            var (isValid, validationErrors) = _validationService.ValidateTriageRecord(triageRecord);
            if (!isValid)
            {
                _logger.LogWarning("TRIAGE_VALIDATION_FAILED: ReferralId={ReferralId}, Errors={Errors}",
                    referralId, string.Join("; ", validationErrors));
                await _deadLetterService.EmitToDeadLetterAsync(
                    referralId,
                    "validation_failed",
                    $"Validation errors: {string.Join("; ", validationErrors)}",
                    retryCount: 0);
                await UpdateReferralStatusAsync(referralId, "failed");
                return;
            }

            // Store triage record in SQL DB
            await StoreTriageRecordAsync(triageRecord);

            // Apply quality gates to determine final status
            var confidenceThreshold = _configuration.GetValue<double>(DEFAULT_CONFIDENCE_THRESHOLD_KEY, DEFAULT_CONFIDENCE_THRESHOLD);
            var finalStatus = ApplyQualityGates(triageResponse, confidenceThreshold, referralId);

            // Update referral status based on quality gates
            await UpdateReferralStatusAsync(referralId, finalStatus);

            stopwatch.Stop();
            _logger.LogInformation("TRIAGE_PIPELINE_COMPLETE: ReferralId={ReferralId}, FinalStatus={Status}, Duration={DurationMs}ms, ConfidenceScore={ConfidenceScore}",
                referralId, finalStatus, stopwatch.ElapsedMilliseconds, triageResponse.ConfidenceScore);
        }
        catch (TriageClassificationException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "TRIAGE_CLASSIFICATION_EXCEPTION: ReferralId={ReferralId}, Duration={DurationMs}ms, Message={Message}",
                referralId, stopwatch.ElapsedMilliseconds, ex.Message);
            await _deadLetterService.EmitToDeadLetterAsync(
                referralId,
                "classification_exception",
                ex.Message,
                retryCount: MAX_RETRIES);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "TRIAGE_PIPELINE_ERROR: ReferralId={ReferralId}, Duration={DurationMs}ms, Exception={ExceptionType}",
                referralId, stopwatch.ElapsedMilliseconds, ex.GetType().Name);
            await _deadLetterService.EmitToDeadLetterAsync(
                referralId,
                "processing_exception",
                ex.Message,
                retryCount: 0);
        }
    }

    /// <summary>
    /// Applies quality gates to determine whether a referral can be auto-completed or requires manual review.
    /// Quality gate criteria:
    /// 1. Confidence score >= configured threshold (default 0.90)
    /// 2. All required extracted fields are populated (non-empty)
    /// </summary>
    private string ApplyQualityGates(TriageResponse triageResponse, double confidenceThreshold, string referralId)
    {
        // Check confidence score threshold
        if (triageResponse.ConfidenceScore < confidenceThreshold)
        {
            _logger.LogInformation(
                "Referral {ReferralId} failed confidence gate: score={Score} < threshold={Threshold}",
                referralId, triageResponse.ConfidenceScore, confidenceThreshold);
            return "pending_review";
        }

        // Check required fields are populated
        if (!AllRequiredFieldsPopulated(triageResponse.ExtractedFields))
        {
            _logger.LogInformation(
                "Referral {ReferralId} failed required fields gate: missing or empty required field",
                referralId);
            return "pending_review";
        }

        _logger.LogInformation(
            "Referral {ReferralId} passed all quality gates: score={Score}, all required fields populated",
            referralId, triageResponse.ConfidenceScore);
        return "completed";
    }

    /// <summary>
    /// Validates that all required extracted fields are present and non-empty.
    /// Required fields: patient_name, dob, symptoms, duration, red_flags
    /// </summary>
    private bool AllRequiredFieldsPopulated(Dictionary<string, string>? extractedFields)
    {
        if (extractedFields == null || extractedFields.Count == 0)
        {
            return false;
        }

        var requiredFields = new[] { "patient_name", "dob", "symptoms", "duration", "red_flags" };

        foreach (var field in requiredFields)
        {
            if (!extractedFields.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
        }

        return true;
    }

    private async Task StoreTriageRecordAsync(Models.TriageRecord triageRecord)
    {
        try
        {
            var referralGuid = Guid.Parse(triageRecord.ReferralId);
            var serializedFields = System.Text.Json.JsonSerializer.Serialize(triageRecord.ExtractedFields);

            // Check if a triage record already exists for this referral (idempotent)
            var existingRecord = await _dbContext.TriageRecords
                .FirstOrDefaultAsync(tr => tr.ReferralId == referralGuid);

            if (existingRecord != null)
            {
                // Update existing record (retry/replay scenario)
                existingRecord.Specialty = triageRecord.Specialty;
                existingRecord.Urgency = triageRecord.Urgency;
                existingRecord.ExtractedFields = serializedFields;
                existingRecord.ClinicalSummary = triageRecord.ClinicalSummary;
                existingRecord.ConfidenceScore = triageRecord.ConfidenceScore.HasValue ? (decimal?)triageRecord.ConfidenceScore : null;
                existingRecord.TriagedAt = triageRecord.TriagedAt;
                existingRecord.ModifiedAt = DateTime.UtcNow;

                _dbContext.TriageRecords.Update(existingRecord);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Triage record updated (idempotent retry) in SQL Server: {ReferralId}", triageRecord.Id);
            }
            else
            {
                // Insert new record
                var triageRecordEntity = new Infrastructure.TriageRecord
                {
                    TriageRecordId = Guid.NewGuid(),
                    ReferralId = referralGuid,
                    Specialty = triageRecord.Specialty,
                    Urgency = triageRecord.Urgency,
                    ExtractedFields = serializedFields,
                    ClinicalSummary = triageRecord.ClinicalSummary,
                    ConfidenceScore = triageRecord.ConfidenceScore.HasValue ? (decimal?)triageRecord.ConfidenceScore : null,
                    CreatedAt = DateTime.UtcNow,
                    TriagedAt = triageRecord.TriagedAt,
                    ModifiedAt = DateTime.UtcNow
                };

                _dbContext.TriageRecords.Add(triageRecordEntity);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Triage record stored in SQL Server: {ReferralId}", triageRecord.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing triage record: {ReferralId}", triageRecord.Id);
            throw;
        }
    }

    private async Task UpdateReferralStatusAsync(string referralId, string status)
    {
        try
        {
            var referralGuid = Guid.Parse(referralId);
            var referral = await _dbContext.Referrals.FindAsync(referralGuid);

            if (referral != null)
            {
                referral.Status = status;
                referral.ModifiedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Referral status updated in SQL Server: {ReferralId}, Status: {Status}", referralId, status);
            }
            else
            {
                _logger.LogWarning("Referral not found for status update: {ReferralId}", referralId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating referral status: {ReferralId}", referralId);
        }
    }
}
