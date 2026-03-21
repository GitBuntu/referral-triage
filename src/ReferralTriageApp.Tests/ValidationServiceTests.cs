using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ReferralTriageApp.Models;
using ReferralTriageApp.Services;
using Xunit;

namespace ReferralTriageApp.Tests;

public class ValidationServiceTests
{
    private readonly ValidationService _validationService;

    public ValidationServiceTests()
    {
        var mockConfig = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<ValidationService>>();
        _validationService = new ValidationService(mockConfig.Object, mockLogger.Object);
    }

    private Models.TriageRecord CreateValidTriageRecord()
    {
        return new Models.TriageRecord
        {
            Id = "rec-123",
            ReferralId = "ref-123",
            Specialty = "cardiology",
            Urgency = "routine",
            ExtractedFields = new Dictionary<string, string>
            {
                { "patient_name", "John Doe" },
                { "dob", "1980-01-15" },
                { "symptoms", "chest pain" },
                { "duration", "2 weeks" },
                { "red_flags", "none" }
            },
            ClinicalSummary = "Patient presents with symptoms",
            OriginalText = "Medical document content",
            TriagedAt = DateTime.UtcNow,
            ConfidenceScore = 0.95
        };
    }

    [Fact]
    public void ValidateTriageRecord_WithValidRecord_ReturnsTrue()
    {
        var record = CreateValidTriageRecord();
        var (isValid, errors) = _validationService.ValidateTriageRecord(record);
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateTriageRecord_WithNullSpecialty_ReturnsFalse()
    {
        var record = CreateValidTriageRecord();
        record.Specialty = null!;
        var (isValid, errors) = _validationService.ValidateTriageRecord(record);
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateTriageRecord_WithNullUrgency_ReturnsFalse()
    {
        var record = CreateValidTriageRecord();
        record.Urgency = null!;
        var (isValid, errors) = _validationService.ValidateTriageRecord(record);
        Assert.False(isValid);
    }
}
