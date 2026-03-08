using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReferralTriageApp.Models;

public class ReferralIntakeRequest
{
    public required string DocumentData { get; set; }
    public required string DocumentFormat { get; set; } // pdf, txt, png, jpg, jpeg
    public string? PatientMRN { get; set; }
}

public class ReferralIntakeResponse
{
    public required string ReferralId { get; set; }
    public string? BlobUri { get; set; }
    public DateTime SubmittedAt { get; set; }
    public required string DocumentFormat { get; set; }
    public string? DocumentHash { get; set; }
    public string? Message { get; set; }
}

public class ReferralDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; set; } // ReferralId
    
    public required string DocumentFormat { get; set; }
    public required string BlobPath { get; set; }
    public required string DocumentHash { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? PatientMRN { get; set; }
    public string? Status { get; set; } // pending, processing, completed, failed
    
    [JsonPropertyName("type")]
    public string Type => "Referral";
    
    [JsonPropertyName("_ts")]
    public long? Timestamp { get; set; }
}

public class TriageRecord
{
    [JsonPropertyName("id")]
    public required string Id { get; set; } // ReferralId
    
    public required string ReferralId { get; set; }
    public required string Specialty { get; set; } // cardiology, orthopaedics, neurology, dermatology, general_medicine
    public required string Urgency { get; set; } // routine, soon, urgent
    
    public required Dictionary<string, string> ExtractedFields { get; set; }
    // Required fields: patient_name, dob, symptoms, duration, red_flags
    
    public required string ClinicalSummary { get; set; } // <500 chars
    public required string OriginalText { get; set; } // Full extracted text from document
    
    public DateTime TriagedAt { get; set; }
    public string? AIModel { get; set; }
    public double? ConfidenceScore { get; set; }
    
    [JsonPropertyName("type")]
    public string Type => "TriageRecord";
    
    [JsonPropertyName("_ts")]
    public long? Timestamp { get; set; }
}

public class TriageRequest
{
    public required string ReferralId { get; set; }
    public required string DocumentFormat { get; set; }
    public required string ExtractedText { get; set; }
}

public class TriageResponse
{
    public required string Specialty { get; set; }
    public required string Urgency { get; set; }
    public required Dictionary<string, string> ExtractedFields { get; set; }
    public required string ClinicalSummary { get; set; }
    public double ConfidenceScore { get; set; }
}

public class DailyMetrics
{
    [JsonPropertyName("id")]
    public required string Id { get; set; } // date-YYYY-MM-DD
    
    public DateTime MetricDate { get; set; }
    public int TotalReferralsProcessed { get; set; }
    
    public Dictionary<string, int> ReferralsBySpecialty { get; set; } = new();
    // e.g., { "cardiology": 5, "neurology": 3, ... }
    
    public int RoutineCount { get; set; }
    public int SoonCount { get; set; }
    public int UrgentCount { get; set; }
    
    public double AverageProcessingLatencyMs { get; set; }
    public Dictionary<string, double> MissingFieldRates { get; set; } = new();
    // e.g., { "red_flags": 0.15, "dob": 0.05, ... }
    
    [JsonPropertyName("type")]
    public string Type => "Metrics";
    
    [JsonPropertyName("_ts")]
    public long? Timestamp { get; set; }
}

public class ValidationError
{
    public required string Field { get; set; }
    public required string Message { get; set; }
}

public class ErrorResponse
{
    public required string Message { get; set; }
    public string? Code { get; set; }
    public List<ValidationError>? Errors { get; set; }
}
