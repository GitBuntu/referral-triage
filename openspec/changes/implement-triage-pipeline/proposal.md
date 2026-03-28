## Why

The ReferralTriage MVP has core infrastructure in place (database schema, EF Core, services, and function triggers), but requires refinement to production readiness. Specifically:

1. **TriageClassificationService** currently uses basic JSON parsing; must be upgraded to GPT-4o **function calling** for guaranteed structured output (eliminates hallucination risks)
2. **Error handling gaps**: No retry logic for transient Document Intelligence/OpenAI failures; no dead-letter queue for failed referrals
3. **Testing coverage**: Minimal test infrastructure; need unit/integration tests to validate the end-to-end pipeline
4. **Configuration**: local.settings.json lacks documented schema; production secrets management needs guidance

This change hardens the MVP by implementing function calling, retry/DLQ patterns, and comprehensive test coverage.

## What Changes

- **Upgrade TriageClassificationService**: Replace JSON parsing with OpenAI function calling (strict schema enforcement for specialty, urgency, extracted fields, confidence score)
- **Add Quality Gates**: Validate confidence_score and required field population before marking triages complete; route low-confidence or incomplete extractions to review queue
- **Implement Retry Logic**: Add max 2 retry attempts with exponential backoff for Document Intelligence and OpenAI calls
- **Add Dead-Letter Queue**: Route failed referrals (after 2 retries) to Azure Storage Queue for manual review
- **Update TriageProcessorFunction**: Integrate retry logic and DLQ handling; enhance error logging
- **Configuration & Secrets**: Document local.settings.json schema; add Key Vault integration for production
- **Test Suite**: Unit tests for ValidationService, DocumentExtractionService, TriageClassificationService; integration tests for end-to-end flow; local smoke test instructions

## Capabilities

### New Capabilities
- `triage-function-calling`: Upgrade TriageClassificationService to use OpenAI function calling (strict schema) instead of JSON parsing; eliminates hallucination risks
- `error-retry-handling`: Max 2 retries with exponential backoff for Document Intelligence and OpenAI calls; routes failures to dead-letter queue
- `dead-letter-queue`: Azure Storage Queue for storing failed referrals (after 2 retries) pending manual review

### Modified Capabilities
- `triage-classification`: Currently uses basic JSON parsing + validation; upgrade to function calling schema enforcement
- `triage-processing`: Add retry logic and DLQ handling to TriageProcessorFunction error path; add quality gates (confidence score + required fields validation)
- `triage-record-persistence`: Add confidence_score field to TriageRecord for decision logic

### Already Complete (No Changes)
- `referral-intake`: HTTP endpoint validates request, uploads document to Blob, stores Referral in SQL (pending status)
- `document-extraction`: Document Intelligence integration extracts text from PDFs/images; fallback for text files
- `referral-persistence`: Referral records stored in SQL with status tracking (pending, triaging, completed, failed)
- `triage-record-persistence`: TriageRecord stored in SQL with ExtractedFields as JSON column
- `metrics-aggregation`: Daily aggregation of counts, latencies, missing field rates stored in DailyMetrics table
- `blob-trigger-processing`: TriageProcessor fires on `/referrals/incoming/{referralId}/*` documents
- `timer-trigger-metrics`: MetricsAggregator runs daily at configured schedule (default 02:00 UTC)

## Impact

- **Code Changes**: Refactor TriageClassificationService (function calling), enhance TriageProcessorFunction (retry + DLQ logic), add retry helpers, add quality gate validation in TriageProcessingService
- **Database**: Add confidence_score column to TriageRecord; update Referral status enum to include 'pending_review'
- **Configuration**: Add local.settings.json schema documentation; add Triage:ConfidenceThreshold setting; update Program.cs for Storage Queue DLQ connection
- **Azure Resources**: Add Azure Storage Queue for dead-letter queue; optionally add review queue for low-confidence triages
- **Testing**: Add unit tests (ValidationService ✅, DocumentExtractionService, TriageClassificationService with function calling) and integration tests; add validation scenario tests
- **Documentation**: Document local.settings.json required keys and values; provide Key Vault secret injection sample; document confidence threshold rationale
