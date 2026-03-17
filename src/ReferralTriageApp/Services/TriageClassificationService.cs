using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Models;

namespace ReferralTriageApp.Services;

public class TriageClassificationService : ITriageClassificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TriageClassificationService> _logger;

    public TriageClassificationService(
        IConfiguration configuration,
        ILogger<TriageClassificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TriageResponse> ClassifyReferralAsync(TriageRequest request)
    {
        try
        {
            _logger.LogInformation("Starting AI classification for referral: {ReferralId}", request.ReferralId);

            var endpoint = _configuration["ReferralTriageSettings:AzureOpenAiEndpoint"];
            var key = _configuration["ReferralTriageSettings:AzureOpenAiKey"];
            var deploymentName = _configuration["ReferralTriageSettings:AzureOpenAiDeploymentName"] ?? "gpt-4";

            _logger.LogInformation("Endpoint loaded: {EndpointLoaded}, Key loaded: {KeyLoaded}, Deployment: {DeploymentName}",
                !string.IsNullOrEmpty(endpoint),
                !string.IsNullOrEmpty(key),
                deploymentName);

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Azure OpenAI credentials not found, returning mock classification");
                return GetMockClassification(request.ExtractedText);
            }

            var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(request.ExtractedText);

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Temperature = 0.7f,
                MaxTokens = 1000,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                }
            };

            var response = await client.GetChatCompletionsAsync(chatCompletionsOptions);

            // Log token usage explicitly
            _logger.LogInformation(
                "GPT-4 Token Usage for Referral {ReferralId} - Prompt Tokens: {PromptTokens}, Completion Tokens: {CompletionTokens}, Total Tokens: {TotalTokens}",
                request.ReferralId,
                response.Value.Usage?.PromptTokens ?? 0,
                response.Value.Usage?.CompletionTokens ?? 0,
                response.Value.Usage?.TotalTokens ?? 0);

            var responseText = response.Value.Choices[0].Message.Content;
            _logger.LogInformation("AI response received for referral: {ReferralId}", request.ReferralId);

            // Parse response
            var triageResponse = ParseAIResponse(responseText);

            return triageResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI classification for referral: {ReferralId}. Exception Message: {ExceptionMessage}", request.ReferralId, ex.Message);
            // Return default/mock classification on error
            return GetMockClassification(request.ExtractedText);
        }
    }

    private string BuildSystemPrompt()
    {
        return @"You are a medical triage specialist AI assistant. Your task is to analyze referral documents and classify them for appropriate care routing.

For each referral, you must:
1. Extract key clinical information (patient name, DOB, symptoms, symptom duration, red flags)
2. Classify the specialty as ONE of: cardiology, orthopaedics, neurology, dermatology, general_medicine
3. Assign urgency level as ONE of: routine, soon, urgent
4. Provide a brief clinical summary (under 500 characters)

CRITICAL RULES:
- Specialty MUST be exactly one of: cardiology, orthopaedics, neurology, dermatology, general_medicine
- Urgency MUST be exactly one of: routine, soon, urgent
- Extract fields MUST include: patient_name, dob, symptoms, duration, red_flags
- All extracted fields MUST have non-empty string values
- Clinical summary MUST NOT exceed 500 characters

Respond with a JSON object containing:
{
  ""specialty"": ""string"",
  ""urgency"": ""string"",
  ""extractedFields"": {
    ""patient_name"": ""string"",
    ""dob"": ""string"",
    ""symptoms"": ""string"",
    ""duration"": ""string"",
    ""red_flags"": ""string""
  },
  ""clinicalSummary"": ""string"",
  ""confidenceScore"": 0.85
}";
    }

    private string BuildUserPrompt(string referralText)
    {
        var maxLength = 3000;
        var truncatedText = referralText.Length > maxLength
            ? referralText.Substring(0, maxLength) + "\n[... truncated due to length ...]"
            : referralText;

        return $@"Please analyze the following referral document and provide triage classification:

---REFERRAL DOCUMENT START---
{truncatedText}
---REFERRAL DOCUMENT END---

Provide your response as a valid JSON object only, with no additional text.";
    }

    private TriageResponse ParseAIResponse(string responseText)
    {
        try
        {
            // Try to extract JSON from response
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var parsed = JsonSerializer.Deserialize<TriageResponseJson>(jsonStr, options);

                if (parsed != null)
                {
                    return new TriageResponse
                    {
                        Specialty = ValidateSpecialty(parsed.Specialty),
                        Urgency = ValidateUrgency(parsed.Urgency),
                        ExtractedFields = parsed.ExtractedFields ?? new(),
                        ClinicalSummary = TruncateSummary(parsed.ClinicalSummary ?? ""),
                        ConfidenceScore = parsed.ConfidenceScore
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI response JSON");
        }

        // Fallback
        return GetMockClassification(responseText);
    }

    private string ValidateSpecialty(string? specialty)
    {
        var allowedSpecialties = _configuration["ReferralTriageSettings:AllowedSpecialties"]
            ?? "cardiology,orthopaedics,neurology,dermatology,general_medicine";
        var specialtyList = allowedSpecialties.Split(',');

        if (!string.IsNullOrEmpty(specialty) && specialtyList.Contains(specialty.ToLowerInvariant()))
        {
            return specialty.ToLowerInvariant();
        }

        return "general_medicine"; // Default fallback
    }

    private string ValidateUrgency(string? urgency)
    {
        var allowedUrgencies = _configuration["ReferralTriageSettings:AllowedUrgencies"]
            ?? "routine,soon,urgent";
        var urgencyList = allowedUrgencies.Split(',');

        if (!string.IsNullOrEmpty(urgency) && urgencyList.Contains(urgency.ToLowerInvariant()))
        {
            return urgency.ToLowerInvariant();
        }

        return "routine"; // Default fallback
    }

    private string TruncateSummary(string summary)
    {
        return summary.Length > 500 ? summary.Substring(0, 500) : summary;
    }

    private TriageResponse GetMockClassification(string referralText)
    {
        // Simple heuristic-based classification for testing
        var hasCardiacKeywords = referralText.Contains("heart", StringComparison.OrdinalIgnoreCase) ||
                                 referralText.Contains("cardiac", StringComparison.OrdinalIgnoreCase) ||
                                 referralText.Contains("chest pain", StringComparison.OrdinalIgnoreCase);

        var hasOrthoKeywords = referralText.Contains("bone", StringComparison.OrdinalIgnoreCase) ||
                              referralText.Contains("fracture", StringComparison.OrdinalIgnoreCase) ||
                              referralText.Contains("orthop", StringComparison.OrdinalIgnoreCase);

        var hasNeuroKeywords = referralText.Contains("brain", StringComparison.OrdinalIgnoreCase) ||
                              referralText.Contains("neuro", StringComparison.OrdinalIgnoreCase) ||
                              referralText.Contains("seizure", StringComparison.OrdinalIgnoreCase);

        var specialty = hasCardiacKeywords ? "cardiology" :
                       hasOrthoKeywords ? "orthopaedics" :
                       hasNeuroKeywords ? "neurology" :
                       "general_medicine";

        var hasUrgentKeywords = referralText.Contains("urgent", StringComparison.OrdinalIgnoreCase) ||
                               referralText.Contains("emergency", StringComparison.OrdinalIgnoreCase) ||
                               referralText.Contains("severe", StringComparison.OrdinalIgnoreCase);

        var urgency = hasUrgentKeywords ? "urgent" : "routine";

        return new TriageResponse
        {
            Specialty = specialty,
            Urgency = urgency,
            ExtractedFields = new Dictionary<string, string>
            {
                { "patient_name", "Not extracted" },
                { "dob", "Not extracted" },
                { "symptoms", referralText.Length > 100 ? referralText.Substring(0, 100) : referralText },
                { "duration", "Unknown" },
                { "red_flags", "None noted" }
            },
            ClinicalSummary = "Patient requires evaluation. Document review indicated need for specialist assessment.",
            ConfidenceScore = 0.65
        };
    }

    private class TriageResponseJson
    {
        [JsonPropertyName("specialty")]
        public string? Specialty { get; set; }

        [JsonPropertyName("urgency")]
        public string? Urgency { get; set; }

        [JsonPropertyName("extractedFields")]
        public Dictionary<string, string>? ExtractedFields { get; set; }

        [JsonPropertyName("clinicalSummary")]
        public string? ClinicalSummary { get; set; }

        [JsonPropertyName("confidenceScore")]
        public double ConfidenceScore { get; set; }
    }
}
