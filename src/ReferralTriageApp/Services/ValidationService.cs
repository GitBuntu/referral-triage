using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Models;

namespace ReferralTriageApp.Services;

public class ValidationService : IValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(IConfiguration configuration, ILogger<ValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public (bool IsValid, List<string> Errors) ValidateReferralIntakeRequest(ReferralIntakeRequest request)
    {
        var errors = new List<string>();

        // Validate DocumentData
        if (string.IsNullOrWhiteSpace(request.DocumentData))
        {
            errors.Add("DocumentData is required");
        }
        else
        {
            try
            {
                var documentBytes = Convert.FromBase64String(request.DocumentData);
                var maxFileSize = _configuration.GetValue<long>("AzureServiceSettings:MaxFileSizeBytes", 52428800);

                if (documentBytes.Length == 0)
                {
                    errors.Add("Document cannot be empty (minimum 1 byte required)");
                }
                else if (documentBytes.Length > maxFileSize)
                {
                    errors.Add($"Document exceeds maximum size of {maxFileSize} bytes");
                }
            }
            catch (FormatException)
            {
                errors.Add("DocumentData must be valid base64-encoded data");
            }
        }

        // Validate DocumentFormat
        if (string.IsNullOrWhiteSpace(request.DocumentFormat))
        {
            errors.Add("DocumentFormat is required");
        }
        else
        {
            var allowedFormats = (_configuration["AzureServiceSettings:AllowedFileTypes"] 
                ?? "pdf,txt,png,jpg,jpeg").Split(',');
            
            if (!allowedFormats.Contains(request.DocumentFormat.ToLowerInvariant()))
            {
                errors.Add($"DocumentFormat '{request.DocumentFormat}' is not supported. Allowed formats: {string.Join(", ", allowedFormats)}");
            }
        }

        var isValid = errors.Count == 0;
        if (!isValid)
        {
            _logger.LogWarning("Referral intake request validation failed: {Errors}", string.Join("; ", errors));
        }

        return (isValid, errors);
    }

    public (bool IsValid, List<string> Errors) ValidateTriageRecord(TriageRecord record)
    {
        var errors = new List<string>();

        // Validate Specialty
        if (string.IsNullOrWhiteSpace(record.Specialty))
        {
            errors.Add("Specialty is required");
        }
        else
        {
            var allowedSpecialties = (_configuration["AzureServiceSettings:AllowedSpecialties"] 
                ?? "cardiology,orthopaedics,neurology,dermatology,general_medicine").Split(',');
            
            if (!allowedSpecialties.Contains(record.Specialty.ToLowerInvariant()))
            {
                errors.Add($"Specialty '{record.Specialty}' is not in allowed list: {string.Join(", ", allowedSpecialties)}");
            }
        }

        // Validate Urgency
        if (string.IsNullOrWhiteSpace(record.Urgency))
        {
            errors.Add("Urgency is required");
        }
        else
        {
            var allowedUrgencies = (_configuration["AzureServiceSettings:AllowedUrgencies"] 
                ?? "routine,soon,urgent").Split(',');
            
            if (!allowedUrgencies.Contains(record.Urgency.ToLowerInvariant()))
            {
                errors.Add($"Urgency '{record.Urgency}' is not in allowed list: {string.Join(", ", allowedUrgencies)}");
            }
        }

        // Validate ExtractedFields
        errors.AddRange(ValidateExtractedFields(record.ExtractedFields));

        // Validate ClinicalSummary
        if (string.IsNullOrWhiteSpace(record.ClinicalSummary))
        {
            errors.Add("ClinicalSummary cannot be empty");
        }
        else if (record.ClinicalSummary.Length > 500)
        {
            errors.Add($"ClinicalSummary exceeds maximum length of 500 characters (current: {record.ClinicalSummary.Length})");
        }

        // Validate OriginalText
        if (string.IsNullOrWhiteSpace(record.OriginalText))
        {
            errors.Add("OriginalText is required");
        }

        var isValid = errors.Count == 0;
        if (!isValid)
        {
            _logger.LogWarning("Triage record validation failed for {ReferralId}: {Errors}", 
                record.Id, string.Join("; ", errors));
        }

        return (isValid, errors);
    }

    private List<string> ValidateExtractedFields(Dictionary<string, string> extractedFields)
    {
        var errors = new List<string>();
        var requiredFields = new[] { "patient_name", "dob", "symptoms", "duration", "red_flags" };

        if (extractedFields == null || extractedFields.Count == 0)
        {
            errors.Add("ExtractedFields cannot be empty");
            return errors;
        }

        foreach (var field in requiredFields)
        {
            if (!extractedFields.ContainsKey(field))
            {
                errors.Add($"Required field '{field}' is missing from ExtractedFields");
            }
            else if (string.IsNullOrWhiteSpace(extractedFields[field]))
            {
                errors.Add($"Required field '{field}' cannot be empty");
            }
        }

        return errors;
    }
}
