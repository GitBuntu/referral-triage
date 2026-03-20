## 1. Upgrade TriageClassificationService to Function Calling

- [ ] 1.1 Review current TriageClassificationService.ParseAIResponse and mock classification fallbacks
- [ ] 1.2 Define function schema for `triage_referral`:
        - specialty (enum: cardiology, orthopaedics, neurology, dermatology, general_medicine)
        - urgency (enum: routine, soon, urgent)
        - extracted_fields (object: patient_name, dob, symptoms, duration, red_flags all required)
        - clinical_summary (string, max 500)
        - confidence_score (number 0-1, optional)
- [ ] 1.3 Create ChatCompletionFunction definition with schema in TriageClassificationService
- [ ] 1.4 Refactor ClassifyReferralAsync to use function calling:
        - Call GetChatCompletionAsync with function; set response_format to function call
        - Check response.FinishReason == CompletionFinishReason.ToolCalls
        - Extract ChatCompletionFunctionToolCall from response.Choices[0]
        - Parse toolCall.Arguments (JSON string) to TriageResponse
- [ ] 1.5 Add exception handling: if function call fails or returns invalid data, throw TriageClassificationException
- [ ] 1.6 Update system prompt to be less prescriptive (function schema handles structure)
- [ ] 1.7 Test: Unit test with mock OpenAIClient function calling response; verify TriageResponse is valid
- [ ] 1.8 Test: Verify ParseAIResponse fallback still works as safety net

## 2. Create Retry Helper Utility

- [ ] 2.1 Create RetryHelper class in Services/ with generic async retry method:
        - Signature: `Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxRetries, ILogger logger, string operationName)`
        - Exponential backoff: 1000ms * 2^attempt (1s, 2s, 4s...)
        - Log retry attempts with attempt number and exception details
        - Return null on final failure (not throw; caller decides handling)
- [ ] 2.2 Test: Unit test RetryAsync with mock operation; verify exponential backoff timing
- [ ] 2.3 Test: Unit test with operation that always fails; verify 2 attempts then null return
- [ ] 2.4 Test: Unit test with operation that fails once, succeeds on retry 2

## 3. Add Storage Queue Dead-Letter Queue

- [ ] 3.1 Register QueueServiceClient in Program.cs DI container (use configuration for connection string)
- [ ] 3.2 Add configuration key `StorageSettings:DLQName` with default value `referral-dlq`
- [ ] 3.3 Create DeadLetterService interface and implementation:
        - Method: `EmitToDeadLetterAsync(string referralId, string failureReason, string errorMessage, int retryCount)`
        - Create message: JSON with referralId, failureReason, errorMessage, timestamp, retryCount
        - Send to Storage Queue DLQ
        - Log message emission with referral ID and reason
- [ ] 3.4 Integrate DeadLetterService into DI container

## 4. Refactor TriageProcessingService with Retry Logic and DLQ

- [ ] 4.1 Inject RetryHelper and DeadLetterService into TriageProcessingService
- [ ] 4.2 Update ProcessTriageAsync to wrap extraction step in RetryAsync:
        - Wrap DocumentExtractionService call in RetryAsync (max 2 retries)
        - If null returned: emit to DLQ with "document_extraction_failed", update status to "failed", return
- [ ] 4.3 Update ProcessTriageAsync to wrap classification step in RetryAsync (same pattern):
        - Wrap TriageClassificationService call in RetryAsync (max 2 retries)
        - If null returned: emit to DLQ with "classification_failed", update status to "failed", return
- [ ] 4.4 Update error handler: on unhandled exception, emit to DLQ if not already attempted
- [ ] 4.5 Add logging: log retry attempts with attempt number, exception type, and DLQ emissions
- [ ] 4.6 Test: Unit test with mock DocumentExtractionService that fails; verify DLQ is called after 2 retries
- [ ] 4.7 Test: Unit test with mock TriageClassificationService that fails; verify DLQ is called

## 5. Update TriageProcessorFunction Error Handling

- [ ] 5.1 Review current Run method error handling; enhance to catch and log BlobTrigger exceptions
- [ ] 5.2 Add try-catch around ProcessTriageAsync call; log detailed error (referral ID, blob path, exception)
- [ ] 5.3 Ensure errors don't cause function to retry automatically (BlobTrigger behavior)
- [ ] 5.4 Log successful completions with referral ID and status (pending → processing → completed/failed)
- [ ] 5.5 Test: Unit test BlobTrigger logic with mock TriageProcessingService that throws exception

## 6. Configuration Schema Documentation

- [ ] 6.1 Create/update local.settings.json template file with all required keys
- [ ] 6.2 Document each config key in README with description and example values
- [ ] 6.3 Add comments to Program.cs noting where each config is used
- [ ] 6.4 Create production Key Vault sample in Bicep/docs showing secret injection syntax

## 7. Unit Tests for ValidationService

- [ ] 7.1 Create test file: ReferralTriageApp.Tests/Services/ValidationServiceTests.cs
- [ ] 7.2 Test ReferralIntakeRequest validation:
        - Valid request → no errors
        - DocumentData > max size → error
        - DocumentData < 1 byte → error
        - Invalid DocumentFormat → error
        - Missing DocumentData → error
        - Missing DocumentFormat → error
- [ ] 7.3 Test TriageRecord validation:
        - Valid record → no errors
        - Invalid specialty (not in enum) → error
        - Invalid urgency (not in enum) → error
        - Missing extracted fields → error
        - ClinicalSummary > 500 chars → error
        - ClinicalSummary empty → error
- [ ] 7.4 Run tests: `dotnet test ReferralTriageApp.Tests` (validation tests pass)

## 8. Unit Tests for DocumentExtractionService

- [ ] 8.1 Create test file: ReferralTriageApp.Tests/Services/DocumentExtractionServiceTests.cs
- [ ] 8.2 Mock DocumentAnalysisClient
- [ ] 8.3 Test successful text extraction from PDF
- [ ] 8.4 Test successful text extraction from text file (no API call)
- [ ] 8.5 Test Document Intelligence API error (throws exception)
- [ ] 8.6 Test fallback for null/empty response
- [ ] 8.7 Test SAS URI generation for blob access
- [ ] 8.8 Run tests: `dotnet test` (all pass)

## 9. Unit Tests for TriageClassificationService (with Function Calling)

- [ ] 9.1 Create test file: ReferralTriageApp.Tests/Services/TriageClassificationServiceTests.cs
- [ ] 9.2 Mock OpenAIClient
- [ ] 9.3 Test successful function calling with valid response
- [ ] 9.4 Test function calling with missing required field (invalid schema)
- [ ] 9.5 Test function calling with invalid specialty enum value
- [ ] 9.6 Test OpenAI API error (throws exception)
- [ ] 9.7 Test mock classification fallback when credentials missing
- [ ] 9.8 Test function signature has correct structure
- [ ] 9.9 Run tests: `dotnet test` (all pass)

## 10. Unit Tests for RetryHelper

- [ ] 10.1 Create test file: ReferralTriageApp.Tests/Helpers/RetryHelperTests.cs
- [ ] 10.2 Test successful operation (no retries needed)
- [ ] 10.3 Test operation fails once, succeeds on retry 2
- [ ] 10.4 Test operation fails for all retries, returns null
- [ ] 10.5 Test exponential backoff timing (verifyapproximately 1s, 2s delays)
- [ ] 10.6 Test logging of retry attempts
- [ ] 10.7 Run tests: `dotnet test` (all pass)

## 11. Unit Tests for DeadLetterService

- [ ] 11.1 Create test file: ReferralTriageApp.Tests/Services/DeadLetterServiceTests.cs
- [ ] 11.2 Mock QueueServiceClient
- [ ] 11.3 Test message emission with correct JSON structure
- [ ] 11.4 Test referral ID and failure reason are captured
- [ ] 11.5 Test timestamp is added
- [ ] 11.6 Test logging on emission
- [ ] 11.7 Test exception handling (queue unavailable, etc.)
- [ ] 11.8 Run tests: `dotnet test` (all pass)

## 12. Documentation

- [ ] 12.1 Update README.md with:
        - "Running Unit Tests" section (`dotnet test`)
        - "Configuration Reference" (all fields with defaults and descriptions)
- [ ] 12.2 Update design.md with any refinements based on implementation

## 13. Final Validation & Commit

- [ ] 13.1 Run full build: `dotnet build` (no errors)
- [ ] 13.2 Run all unit tests: `dotnet test` (all pass)
- [ ] 13.3 Verify files created/updated:
        - ✅ TriageClassificationService.cs (function calling)
        - ✅ RetryHelper.cs (new)
        - ✅ DeadLetterService.cs (new)
        - ✅ TriageProcessingService.cs (enhanced)
        - ✅ TriageProcessorFunction.cs (enhanced logging)
        - ✅ Program.cs (DI registration)
        - ✅ local.settings.json (template)
        - ✅ Tests: ValidationServiceTests, DocumentExtractionServiceTests, TriageClassificationServiceTests, RetryHelperTests, DeadLetterServiceTests
        - ✅ README.md (test instructions)
- [ ] 13.4 Git commit: `git add -A && git commit -m "feat(pipeline): implement function calling, retry logic, and DLQ"`
- [ ] 13.5 Push to branch: `git push origin 001-implement-specforge-tasks`
