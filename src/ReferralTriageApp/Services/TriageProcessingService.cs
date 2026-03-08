using System;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Models;
using ReferralTriageApp.Infrastructure;

namespace ReferralTriageApp.Services;

public class TriageProcessingService : ITriageProcessingService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ReferralTriageContext _dbContext;
    private readonly IDocumentExtractionService _documentExtractionService;
    private readonly ITriageClassificationService _triageClassificationService;
    private readonly IValidationService _validationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TriageProcessingService> _logger;

    public TriageProcessingService(
        BlobServiceClient blobServiceClient,
        ReferralTriageContext dbContext,
        IDocumentExtractionService documentExtractionService,
        ITriageClassificationService triageClassificationService,
        IValidationService validationService,
        IConfiguration configuration,
        ILogger<TriageProcessingService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _dbContext = dbContext;
        _documentExtractionService = documentExtractionService;
        _triageClassificationService = triageClassificationService;
        _validationService = validationService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ProcessTriageAsync(string referralId, string documentFormat, string blobPath)
    {
        try
        {
            _logger.LogInformation("Starting triage processing for referral: {ReferralId}", referralId);

            // Extract text from document (OCR if needed)
            var extractedText = await _documentExtractionService.ExtractTextFromDocumentAsync(blobPath, documentFormat);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogError("Failed to extract text from document: {ReferralId}", referralId);
                await UpdateReferralStatusAsync(referralId, "failed");
                return;
            }

            // Classify with AI model
            var triageRequest = new TriageRequest
            {
                ReferralId = referralId,
                DocumentFormat = documentFormat,
                ExtractedText = extractedText
            };

            var triageResponse = await _triageClassificationService.ClassifyReferralAsync(triageRequest);

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
                _logger.LogError("Triage record validation failed for {ReferralId}: {Errors}",
                    referralId, string.Join(", ", validationErrors));
                await UpdateReferralStatusAsync(referralId, "failed");
                return;
            }

            // Store triage record in SQL DB
            await StoreTriageRecordAsync(triageRecord);

            // Update referral status
            await UpdateReferralStatusAsync(referralId, "completed");

            _logger.LogInformation("Triage processing completed successfully for referral: {ReferralId}", referralId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during triage processing for referral: {ReferralId}", referralId);
            await UpdateReferralStatusAsync(referralId, "failed");
            throw;
        }
    }

    private async Task StoreTriageRecordAsync(Models.TriageRecord triageRecord)
    {
        try
        {
            var triageRecordEntity = new Infrastructure.TriageRecord
            {
                TriageRecordId = Guid.NewGuid(),
                ReferralId = Guid.Parse(triageRecord.ReferralId),
                Specialty = triageRecord.Specialty,
                Urgency = triageRecord.Urgency,
                ExtractedFields = System.Text.Json.JsonSerializer.Serialize(triageRecord.ExtractedFields),
                ClinicalSummary = triageRecord.ClinicalSummary,
                CreatedAt = DateTime.UtcNow,
                TriagedAt = triageRecord.TriagedAt,
                ModifiedAt = DateTime.UtcNow
            };

            _dbContext.TriageRecords.Add(triageRecordEntity);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Triage record stored in SQL Server: {ReferralId}", triageRecord.Id);
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
