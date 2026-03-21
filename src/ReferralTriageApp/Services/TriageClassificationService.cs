using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Configuration and API constants
    private const string DEFAULT_DEPLOYMENT_NAME = "gpt-4";
    private const string FUNCTION_CALL_FINISH_REASON = "tool_calls";
    private const string FUNCTION_NAME = "triage_referral";

    // API request parameters
    private const float TEMPERATURE = 0.7f;
    private const int MAX_TOKENS = 1000;

    // Confidence score constraints
    private const double DEFAULT_CONFIDENCE_SCORE = 0.5;
    private const double MOCK_CONFIDENCE_SCORE = 0.65;
    private const double CONFIDENCE_MINIMUM = 0;
    private const double CONFIDENCE_MAXIMUM = 1;

    // Text length constraints
    private const int CLINICAL_SUMMARY_MAX_LENGTH = 500;
    private const int USER_PROMPT_MAX_LENGTH = 3000;
    private const int MOCK_SYMPTOMS_TRUNCATE_LENGTH = 100;

    // Default values
    private const string DEFAULT_SPECIALTY = "general_medicine";
    private const string DEFAULT_URGENCY = "routine";
    private const string MOCK_PATIENT_NAME = "Not extracted";
    private const string MOCK_DOB = "Not extracted";
    private const string MOCK_DURATION = "Unknown";
    private const string MOCK_RED_FLAGS = "None noted";
    private const string MOCK_CLINICAL_SUMMARY = "Patient requires evaluation. Document review indicated need for specialist assessment.";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("Starting AI classification for referral: {ReferralId}, TextLength: {TextLength}",
                request.ReferralId, request.ExtractedText?.Length ?? 0);

            var endpoint = _configuration["ReferralTriageApp:AzureOpenAiEndpoint"];
            var key = _configuration["ReferralTriageApp:AzureOpenAiKey"];
            var deploymentName = _configuration["ReferralTriageApp:AzureOpenAiDeploymentName"] ?? DEFAULT_DEPLOYMENT_NAME;

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("CLASSIFICATION_MOCK_FALLBACK: ReferralId={ReferralId}, Azure OpenAI credentials not found, using mock classification",
                    request.ReferralId);
                return GetMockClassification(request.ExtractedText);
            }

            _logger.LogInformation("Azure OpenAI endpoint available for ReferralId={ReferralId}, Deployment={DeploymentName}",
                request.ReferralId, deploymentName);

            var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(request.ExtractedText);

            var triageFunction = new ChatCompletionsFunctionToolDefinition()
            {
                Name = FUNCTION_NAME,
                Description = "Classify a medical referral and extract key clinical information",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        specialty = new
                        {
                            type = "string",
                            description = "Medical specialty",
                            @enum = new[] { "cardiology", "orthopaedics", "neurology", "dermatology", "general_medicine" }
                        },
                        urgency = new
                        {
                            type = "string",
                            description = "Urgency level",
                            @enum = new[] { "routine", "soon", "urgent" }
                        },
                        extracted_fields = new
                        {
                            type = "object",
                            description = "Extracted clinical information",
                            properties = new
                            {
                                patient_name = new { type = "string", description = "Patient full name" },
                                dob = new { type = "string", description = "Date of birth (YYYY-MM-DD format)" },
                                symptoms = new { type = "string", description = "Primary symptoms" },
                                duration = new { type = "string", description = "Symptom duration" },
                                red_flags = new { type = "string", description = "Critical red flags or complications" }
                            },
                            required = new[] { "patient_name", "dob", "symptoms", "duration", "red_flags" }
                        },
                        clinical_summary = new
                        {
                            type = "string",
                            description = "Brief clinical summary (max 500 characters)",
                            maxLength = CLINICAL_SUMMARY_MAX_LENGTH
                        },
                        confidence_score = new
                        {
                            type = "number",
                            description = "Confidence score (0-1) in the classification",
                            minimum = CONFIDENCE_MINIMUM,
                            maximum = CONFIDENCE_MAXIMUM
                        }
                    },
                    required = new[] { "specialty", "urgency", "extracted_fields", "clinical_summary" }
                })
            };

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Temperature = TEMPERATURE,
                MaxTokens = MAX_TOKENS,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                Tools = { triageFunction }
            };

            var response = await client.GetChatCompletionsAsync(chatCompletionsOptions);

            _logger.LogInformation(
                "GPT-4 Token Usage for Referral {ReferralId} - Prompt Tokens: {PromptTokens}, Completion Tokens: {CompletionTokens}, Total Tokens: {TotalTokens}",
                request.ReferralId,
                response.Value.Usage?.PromptTokens ?? 0,
                response.Value.Usage?.CompletionTokens ?? 0,
                response.Value.Usage?.TotalTokens ?? 0);

            // Check if we got a function call
            if (response.Value.Choices[0].FinishReason == FUNCTION_CALL_FINISH_REASON)
            {
                var toolCall = response.Value.Choices[0].Message.ToolCalls[0];

                if (toolCall is ChatCompletionsFunctionToolCall functionToolCall)
                {
                    _logger.LogInformation("Function call received for referral: {ReferralId}", request.ReferralId);

                    try
                    {
                        var triageResponse = ParseFunctionCallResponse(functionToolCall.Arguments);
                        _logger.LogInformation("Successfully parsed function call for referral: {ReferralId}", request.ReferralId);
                        return triageResponse;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing function call arguments for referral: {ReferralId}", request.ReferralId);
                        throw new TriageClassificationException($"Function call response parsing failed for referral {request.ReferralId}", ex);
                    }
                }
            }

            // Fallback: try to parse as regular text response (safety net)
            var responseText = response.Value.Choices[0].Message.Content;
            if (!string.IsNullOrEmpty(responseText))
            {
                _logger.LogInformation("No function call received, attempting fallback JSON parsing for referral: {ReferralId}", request.ReferralId);
                return ParseAIResponse(responseText);
            }

            throw new TriageClassificationException($"No valid response received from OpenAI for referral {request.ReferralId}");
        }
        catch (TriageClassificationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI classification for referral: {ReferralId}. Exception Message: {ExceptionMessage}", request.ReferralId, ex.Message);
            throw new TriageClassificationException($"AI classification failed for referral {request.ReferralId}", ex);
        }
    }

    private TriageResponse ParseFunctionCallResponse(string functionArguments)
    {
        var parsed = JsonSerializer.Deserialize<TriageResponseJson>(functionArguments, JsonOptions);

        if (parsed == null)
        {
            throw new InvalidOperationException("Function arguments deserialization returned null");
        }

        var extractedFields = parsed.ExtractedFields ?? [];

        // Validate all required fields are present and non-empty
        var requiredFields = new[] { "patient_name", "dob", "symptoms", "duration", "red_flags" };
        foreach (var field in requiredFields)
        {
            if (!extractedFields.TryGetValue(field, out var fieldValue) || string.IsNullOrWhiteSpace(fieldValue))
            {
                throw new InvalidOperationException($"Required extracted field '{field}' is missing or empty");
            }
        }

        return new TriageResponse
        {
            Specialty = ValidateSpecialty(parsed.Specialty),
            Urgency = ValidateUrgency(parsed.Urgency),
            ExtractedFields = extractedFields,
            ClinicalSummary = TruncateSummary(parsed.ClinicalSummary ?? ""),
            ConfidenceScore = parsed.ConfidenceScore > 0 ? parsed.ConfidenceScore : DEFAULT_CONFIDENCE_SCORE
        };
    }

    private static string BuildSystemPrompt()
    {
        return @"You are a medical triage specialist AI assistant. Your task is to analyze referral documents and classify them for appropriate care routing.

For each referral, extract key clinical information and classify appropriately:
- Extract patient demographic and clinical details (name, DOB, symptoms, symptom duration, critical red flags)
- Classify the appropriate medical specialty
- Assign urgency level based on clinical indicators
- Provide a concise clinical summary

Focus on accuracy in specialty classification and urgency assessment. The structured format ensures consistency in downstream processing.";
    }

    private static string BuildUserPrompt(string referralText)
    {
        const int maxLength = USER_PROMPT_MAX_LENGTH;
        var truncatedText = referralText.Length > maxLength
            ? string.Concat(referralText.AsSpan(0, maxLength), "\n[... truncated due to length ...]")
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
                var parsed = JsonSerializer.Deserialize<TriageResponseJson>(jsonStr, JsonOptions);

                if (parsed != null)
                {
                    return new TriageResponse
                    {
                        Specialty = ValidateSpecialty(parsed.Specialty),
                        Urgency = ValidateUrgency(parsed.Urgency),
                        ExtractedFields = parsed.ExtractedFields ?? [],
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
        var allowedSpecialties = _configuration["ReferralTriageApp:AllowedSpecialties"]
            ?? "cardiology,orthopaedics,neurology,dermatology,general_medicine";
        var specialtyList = allowedSpecialties.Split(',');

        if (!string.IsNullOrEmpty(specialty) && specialtyList.Contains(specialty.ToLowerInvariant()))
        {
            return specialty.ToLowerInvariant();
        }

        return DEFAULT_SPECIALTY;
    }

    private string ValidateUrgency(string? urgency)
    {
        var allowedUrgencies = _configuration["ReferralTriageApp:AllowedUrgencies"]
            ?? "routine,soon,urgent";
        var urgencyList = allowedUrgencies.Split(',');

        if (!string.IsNullOrEmpty(urgency) && urgencyList.Contains(urgency.ToLowerInvariant()))
        {
            return urgency.ToLowerInvariant();
        }

        return DEFAULT_URGENCY;
    }

    private static string TruncateSummary(string summary)
    {
        return summary.Length > CLINICAL_SUMMARY_MAX_LENGTH
            ? new string(summary.AsSpan(0, CLINICAL_SUMMARY_MAX_LENGTH))
            : summary;
    }

    private static TriageResponse GetMockClassification(string referralText)
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
                { "patient_name", MOCK_PATIENT_NAME },
                { "dob", MOCK_DOB },
                { "symptoms", referralText.Length > MOCK_SYMPTOMS_TRUNCATE_LENGTH ? new string(referralText.AsSpan(0, MOCK_SYMPTOMS_TRUNCATE_LENGTH)) : referralText },
                { "duration", MOCK_DURATION },
                { "red_flags", MOCK_RED_FLAGS }
            },
            ClinicalSummary = MOCK_CLINICAL_SUMMARY,
            ConfidenceScore = MOCK_CONFIDENCE_SCORE
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
