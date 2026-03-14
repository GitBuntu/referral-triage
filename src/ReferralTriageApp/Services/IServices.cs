using ReferralTriageApp.Models;

namespace ReferralTriageApp.Services;

public interface IReferralIntakeService
{
    Task<ReferralIntakeResponse> ProcessReferralAsync(ReferralIntakeRequest request);
}

public interface IDocumentExtractionService
{
    Task<string> ExtractTextFromDocumentAsync(string blobPath, string documentFormat);
}

public interface ITriageProcessingService
{
    /// <summary>
    /// Processes triage based on a document stored in blob storage.
    /// </summary>
    /// <param name="referralId">The referral identifier.</param>
    /// <param name="documentFormat">The document format (pdf, txt, png, jpg, jpeg).</param>
    /// <param name="blobPath">The blob storage path to the uploaded referral document (e.g., referrals/incoming/{referralId}/{fileName}).</param>
    /// <remarks>
    /// This method will:
    /// 1. Extract text from the document at the specified blob path (via IDocumentExtractionService)
    /// 2. Classify the referral using AI (via ITriageClassificationService)
    /// 3. Validate the triage record against domain invariants
    /// 4. Store the result in the database
    /// 5. Update the referral status accordingly
    /// </remarks>
    Task ProcessTriageAsync(string referralId, string documentFormat, string blobPath);
}

public interface ITriageClassificationService
{
    Task<TriageResponse> ClassifyReferralAsync(TriageRequest request);
}

public interface IMetricsAggregationService
{
    Task AggregateMetricsAsync(DateTime metricsDate);
}

public interface IValidationService
{
    (bool IsValid, List<string> Errors) ValidateReferralIntakeRequest(ReferralIntakeRequest request);
    (bool IsValid, List<string> Errors) ValidateTriageRecord(TriageRecord record);
}
