namespace ReferralTriageApp.Infrastructure;

public class AzureServiceSettings
{
    public string? BlobStorageAccount { get; set; }
    public string? BlobContainer { get; set; }
    public string? SqlServerDatabase { get; set; }
    public string? TriageRecordsTableName { get; set; }
    public string? DocumentIntelligenceEndpoint { get; set; }
    public string? DocumentIntelligenceKey { get; set; }
    public string? AzureOpenAiEndpoint { get; set; }
    public string? AzureOpenAiKey { get; set; }
    public string? AzureOpenAiDeploymentName { get; set; }
    public string? MetricsTableName { get; set; }
    public string? AllowedSpecialties { get; set; }
    public string? AllowedUrgencies { get; set; }
    public long MaxFileSizeBytes { get; set; } = 52428800; // 50 MB
    public string? AllowedFileTypes { get; set; }
    public string? MetricsAggregationSchedule { get; set; }
}
