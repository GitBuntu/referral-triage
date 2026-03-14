using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Services;

namespace ReferralTriageApp.Functions;

public class TriageProcessorFunction
{
    private readonly ITriageProcessingService _triageProcessingService;
    private readonly ILogger<TriageProcessorFunction> _logger;

    public TriageProcessorFunction(
        ITriageProcessingService triageProcessingService,
        ILogger<TriageProcessorFunction> logger)
    {
        _triageProcessingService = triageProcessingService;
        _logger = logger;
    }

    [Function("TriageProcessor")]
    public async Task Run(
        [BlobTrigger("referrals/incoming/{referralId}/{fileName}")] Stream blobStream,
        string referralId,
        string fileName,
        FunctionContext context)
    {
        try
        {
            _logger.LogInformation(
                "TriageProcessor triggered for referral: {ReferralId}, file: {FileName}",
                referralId, fileName);

            // Determine document format from file extension
            var documentFormat = GetDocumentFormat(fileName);

            // The blob path for retrieval
            var blobPath = $"referrals/incoming/{referralId}/{fileName}";

            // Process triage
            await _triageProcessingService.ProcessTriageAsync(referralId, documentFormat, blobPath);

            _logger.LogInformation("TriageProcessor completed successfully for referral: {ReferralId}", referralId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TriageProcessor for referral: {ReferralId}", referralId);
            // In production, consider dead-lettering or retry logic
            throw;
        }
    }

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
