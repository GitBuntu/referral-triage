using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Services;

namespace ReferralTriageApp.Functions;

public class TriageProcessorFunction
{
    private readonly ITriageProcessingService _triageProcessingService;
    private readonly IDeadLetterService _deadLetterService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TriageProcessorFunction> _logger;

    public TriageProcessorFunction(
        ITriageProcessingService triageProcessingService,
        IDeadLetterService deadLetterService,
        IConfiguration configuration,
        ILogger<TriageProcessorFunction> logger)
    {
        _triageProcessingService = triageProcessingService;
        _deadLetterService = deadLetterService;
        _configuration = configuration;
        _logger = logger;
    }

    [Function("TriageProcessor")]
    public async Task Run(
        [BlobTrigger("referrals/incoming/{referralId}/{fileName}")] Stream blobStream,
        string referralId,
        string fileName,
        FunctionContext context)
    {
        // Note: BlobTrigger binding is hard-coded to "referrals/incoming/" path.
        // If ReferralTriageApp:BlobIncomingPath configuration changes, ensure this trigger path is updated accordingly.
        // TODO: Consider making trigger path configurable via binding expression if deployment flexibility is needed.

        // Resolve the actual blob path using configuration (accessible in error handlers)
        var blobIncomingPath = _configuration["ReferralTriageApp:BlobIncomingPath"] ?? "incoming";
        var resolvedBlobPath = $"{blobIncomingPath}/{referralId}/{fileName}";

        try
        {
            _logger.LogInformation(
                "TriageProcessor triggered for referral: {ReferralId}, file: {FileName}, blobPath: {ResolvedBlobPath}",
                referralId, fileName, resolvedBlobPath);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(referralId))
            {
                _logger.LogError("Invalid referral ID: {ReferralId}", referralId);
                await _deadLetterService.EmitToDeadLetterAsync(
                    referralId ?? "unknown",
                    "invalid_input",
                    "Referral ID is null, empty, or whitespace",
                    retryCount: 0);
                return;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogError("Invalid file name for referral {ReferralId}", referralId);
                await _deadLetterService.EmitToDeadLetterAsync(
                    referralId,
                    "invalid_input",
                    "File name is null, empty, or whitespace",
                    retryCount: 0);
                return;
            }

            // Determine document format from file extension
            var documentFormat = GetDocumentFormat(fileName);

            if (documentFormat == "unknown")
            {
                _logger.LogWarning(
                    "Unknown document format for referral {ReferralId}: {FileName}",
                    referralId, fileName);
                await _deadLetterService.EmitToDeadLetterAsync(
                    referralId,
                    "unsupported_document_format",
                    $"File extension not recognized: {Path.GetExtension(fileName)}",
                    retryCount: 0);
                return;
            }



            // Process triage (TriageProcessingService handles its own retry/DLQ logic)
            await _triageProcessingService.ProcessTriageAsync(referralId, documentFormat, resolvedBlobPath);

            _logger.LogInformation(
                "TriageProcessor completed successfully for referral: {ReferralId}",
                referralId);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            // Blob not found - application error, not infrastructure failure
            _logger.LogError(
                ex,
                "Blob not found for referral {ReferralId}: {ResolvedBlobPath}. Status: {Status}",
                referralId, resolvedBlobPath, ex.Status);
            await _deadLetterService.EmitToDeadLetterAsync(
                referralId,
                "blob_not_found",
                $"Document blob not found: {resolvedBlobPath}",
                retryCount: 0);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            // Access denied - permission/authentication issue
            _logger.LogError(
                ex,
                "Access denied to blob for referral {ReferralId}: {ResolvedBlobPath}. Status: {Status}",
                referralId, resolvedBlobPath, ex.Status);
            await _deadLetterService.EmitToDeadLetterAsync(
                referralId,
                "blob_access_denied",
                $"Access denied to document blob: {resolvedBlobPath}",
                retryCount: 0);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status >= 500)
        {
            // Server error - transient failure, re-throw to trigger Azure Functions retry policy
            _logger.LogError(
                ex,
                "Azure service error accessing blob for referral {ReferralId}: {ResolvedBlobPath}. Status: {Status}",
                referralId, resolvedBlobPath, ex.Status);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            // Timeout or cancellation
            _logger.LogError(
                ex,
                "Operation timed out or was canceled for referral {ReferralId}",
                referralId);
            await _deadLetterService.EmitToDeadLetterAsync(
                referralId,
                "operation_timeout",
                $"Triage processing timeout or cancellation: {ex.Message}",
                retryCount: 0);
        }
        catch (TriageClassificationException ex)
        {
            // Classification-specific error (already logged in TriageProcessingService)
            _logger.LogError(
                ex,
                "Classification error for referral {ReferralId}",
                referralId);
            // DLQ emission already handled by TriageProcessingService
        }
        catch (Exception ex)
        {
            // Unexpected error
            _logger.LogError(
                ex,
                "Unexpected error in TriageProcessor for referral {ReferralId}: {ExceptionType}: {Message}",
                referralId, ex.GetType().Name, ex.Message);
            // Emit to DLQ to prevent message loss
            await _deadLetterService.EmitToDeadLetterAsync(
                referralId,
                "unexpected_error",
                $"Unexpected error: {ex.GetType().Name}: {ex.Message}",
                retryCount: 0);
            // Don't re-throw - error is captured in DLQ; re-throwing could cause infinite retries
        }
    }

    /// <summary>
    /// Determines the document format from the file extension.
    /// </summary>
    /// <param name="fileName">The name of the document file</param>
    /// <returns>The document format string (e.g., "pdf", "txt"), or "unknown" if not supported</returns>
    private string GetDocumentFormat(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        return extension switch
        {
            "pdf" => "pdf",
            "txt" => "txt",
            "text" => "txt",
            "png" => "png",
            "jpg" => "jpg",
            "jpeg" => "jpg",
            _ => "unknown"
        };
    }
}
