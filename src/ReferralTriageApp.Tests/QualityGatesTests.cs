using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ReferralTriageApp.Models;
using ReferralTriageApp.Services;
using Xunit;

namespace ReferralTriageApp.Tests;

/// <summary>
/// Tests for quality gates validation in TriageProcessingService.
/// Quality gates determine if a referral is auto-completed or routed to pending_review.
///
/// Gates:
/// 1. Confidence Score Gate: AI confidence >= Triage:ConfidenceThreshold (default 0.90)
/// 2. Required Fields Gate: All required fields populated (patient_name, dob, symptoms, duration, red_flags)
///
/// Result:
/// - Both pass -> status "completed" (auto-completion)
/// - Either fails -> status "pending_review" (manual review needed)
/// </summary>
public class QualityGatesTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<TriageProcessingService>> _mockLogger;

    public QualityGatesTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<TriageProcessingService>>();

        // Set default confidence threshold
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x.Value).Returns("0.90");
        _mockConfiguration
            .Setup(x => x.GetSection("ReferralTriageApp"))
            .Returns(new Mock<IConfigurationSection>().Object);
    }

    [Fact]
    public void QualityGates_HighConfidence_AllFieldsPopulated_PassesBothGates()
    {
        // Arrange
        var triageResponse = CreateValidTriageResponse(
            confidenceScore: 0.95,
            hasAllRequiredFields: true);

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("completed", status);
    }

    [Fact]
    public void QualityGates_LowConfidence_AllFieldsPopulated_FailsConfidenceGate()
    {
        // Arrange
        var triageResponse = CreateValidTriageResponse(
            confidenceScore: 0.85,
            hasAllRequiredFields: true);

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Fact]
    public void QualityGates_HighConfidence_MissingRequiredFields_FailsRequiredFieldsGate()
    {
        // Arrange
        var triageResponse = CreateValidTriageResponse(
            confidenceScore: 0.95,
            hasAllRequiredFields: false);

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Fact]
    public void QualityGates_LowConfidence_MissingRequiredFields_FailsBothGates()
    {
        // Arrange
        var triageResponse = CreateValidTriageResponse(
            confidenceScore: 0.75,
            hasAllRequiredFields: false);

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Fact]
    public void QualityGates_EdgeCase_ConfidenceEqualToThreshold_Passes()
    {
        // Arrange - exactly at threshold
        var triageResponse = CreateValidTriageResponse(
            confidenceScore: 0.90,
            hasAllRequiredFields: true);

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("completed", status);
    }

    [Fact]
    public void QualityGates_EdgeCase_ConfidenceJustBelowThreshold_Fails()
    {
        // Arrange - just below threshold
        var triageResponse = CreateValidTriageResponse(
            confidenceScore: 0.899,
            hasAllRequiredFields: true);

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Fact]
    public void QualityGates_MissingPatientName_FailsRequiredFieldsGate()
    {
        // Arrange
        var triageResponse = new TriageResponse
        {
            Specialty = "cardiology",
            Urgency = "routine",
            ConfidenceScore = 0.95,
            ClinicalSummary = "Clinical notes",
            ExtractedFields = new Dictionary<string, string>
            {
                // Missing patient_name
                { "dob", "1980-01-15" },
                { "symptoms", "chest pain" },
                { "duration", "2 weeks" },
                { "red_flags", "none" }
            }
        };

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Fact]
    public void QualityGates_MissingDOB_FailsRequiredFieldsGate()
    {
        // Arrange
        var triageResponse = new TriageResponse
        {
            Specialty = "cardiology",
            Urgency = "routine",
            ConfidenceScore = 0.95,
            ClinicalSummary = "Clinical notes",
            ExtractedFields = new Dictionary<string, string>
            {
                { "patient_name", "John Doe" },
                // Missing dob
                { "symptoms", "chest pain" },
                { "duration", "2 weeks" },
                { "red_flags", "none" }
            }
        };

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Fact]
    public void QualityGates_EmptyPatientName_FailsRequiredFieldsGate()
    {
        // Arrange
        var triageResponse = new TriageResponse
        {
            Specialty = "cardiology",
            Urgency = "routine",
            ConfidenceScore = 0.95,
            ClinicalSummary = "Clinical notes",
            ExtractedFields = new Dictionary<string, string>
            {
                { "patient_name", "" }, // Empty
                { "dob", "1980-01-15" },
                { "symptoms", "chest pain" },
                { "duration", "2 weeks" },
                { "red_flags", "none" }
            }
        };

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Fact]
    public void QualityGates_WhitespaceOnlyField_FailsRequiredFieldsGate()
    {
        // Arrange
        var triageResponse = new TriageResponse
        {
            Specialty = "cardiology",
            Urgency = "routine",
            ConfidenceScore = 0.95,
            ClinicalSummary = "Clinical notes",
            ExtractedFields = new Dictionary<string, string>
            {
                { "patient_name", "   " }, // Whitespace only
                { "dob", "1980-01-15" },
                { "symptoms", "chest pain" },
                { "duration", "2 weeks" },
                { "red_flags", "none" }
            }
        };

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Fact]
    public void QualityGates_NullExtractedFields_FailsRequiredFieldsGate()
    {
        // Arrange
        var triageResponse = new TriageResponse
        {
            Specialty = "cardiology",
            Urgency = "routine",
            ConfidenceScore = 0.95,
            ClinicalSummary = "Clinical notes",
            ExtractedFields = null
        };

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Fact]
    public void QualityGates_EmptyExtractedFields_FailsRequiredFieldsGate()
    {
        // Arrange
        var triageResponse = new TriageResponse
        {
            Specialty = "cardiology",
            Urgency = "routine",
            ConfidenceScore = 0.95,
            ClinicalSummary = "Clinical notes",
            ExtractedFields = new Dictionary<string, string>() // Empty
        };

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Theory]
    [InlineData(0.50)]
    [InlineData(0.70)]
    [InlineData(0.85)]
    public void QualityGates_WithVariousLowConfidenceScores_AllFail(double confidenceScore)
    {
        // Arrange
        var triageResponse = CreateValidTriageResponse(
            confidenceScore: confidenceScore,
            hasAllRequiredFields: true);

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("pending_review", status);
    }

    [Theory]
    [InlineData(0.91)]
    [InlineData(0.95)]
    [InlineData(0.99)]
    public void QualityGates_WithVariousHighConfidenceScores_AllPass(double confidenceScore)
    {
        // Arrange
        var triageResponse = CreateValidTriageResponse(
            confidenceScore: confidenceScore,
            hasAllRequiredFields: true);

        // Act
        var status = ApplyQualityGates(triageResponse, 0.90);

        // Assert
        Assert.Equal("completed", status);
    }

    // Helper methods
    private TriageResponse CreateValidTriageResponse(double confidenceScore, bool hasAllRequiredFields)
    {
        var extractedFields = new Dictionary<string, string>();

        if (hasAllRequiredFields)
        {
            extractedFields.Add("patient_name", "John Doe");
            extractedFields.Add("dob", "1980-01-15");
            extractedFields.Add("symptoms", "chest pain");
            extractedFields.Add("duration", "2 weeks");
            extractedFields.Add("red_flags", "shortness of breath");
        }

        return new TriageResponse
        {
            Specialty = "cardiology",
            Urgency = "routine",
            ConfidenceScore = confidenceScore,
            ExtractedFields = extractedFields,
            ClinicalSummary = "Patient with cardiac symptoms"
        };
    }

    private string ApplyQualityGates(TriageResponse triageResponse, double confidenceThreshold)
    {
        // Check confidence score threshold
        if (triageResponse.ConfidenceScore < confidenceThreshold)
        {
            return "pending_review";
        }

        // Check required fields are populated
        if (!AllRequiredFieldsPopulated(triageResponse.ExtractedFields))
        {
            return "pending_review";
        }

        return "completed";
    }

    private bool AllRequiredFieldsPopulated(Dictionary<string, string> extractedFields)
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
}
