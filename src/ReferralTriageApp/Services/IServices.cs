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
    Task ProcessTriageAsync(string referralId, string documentFormat, string extractedText);
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
