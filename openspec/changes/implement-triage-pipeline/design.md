## Context

The ReferralTriage MVP has most infrastructure implemented:
- ✅ SQL Server schema + EF Core context
- ✅ ReferralIntakeService (HTTP intake → Blob + SQL)
- ✅ DocumentExtractionService (Document Intelligence integration)
- ✅ TriageProcessingService (orchestration)
- ✅ TriageClassificationService (OpenAI integration - but using basic JSON parsing, not function calling)
- ✅ MetricsAggregationService (daily aggregation)
- ✅ ReferralIntakeFunction (HTTP), TriageProcessorFunction (BlobTrigger), MetricsAggregatorFunction (TimerTrigger)
- ✅ ValidationService

**Remaining Work:**
1. Upgrade TriageClassificationService to use function calling instead of JSON parsing
2. Implement retry logic (max 2, exponential backoff) with dead-letter queue
3. Add comprehensive test coverage (unit + integration)

**Constraints:**
- Documents < 2 MB (sync calls, no async job tracking)
- Max 2 retries before DLQ
- Sequential service calls
- Daily metrics aggregation (yesterday's data)
- All failures (extraction or classification) go to DLQ

## Goals / Non-Goals

**Goals:**
- Upgrade TriageClassificationService to use GPT-4o function calling for guaranteed structured output
- Implement production-grade error handling: max 2 retries with exponential backoff + DLQ routing
- Add comprehensive test coverage (unit + integration tests)
- Document configuration (local.settings.json schema, Key Vault integration)
- Ensure end-to-end reliability: failed referrals are captured and logged for manual review

**Non-Goals:**
- Implement manual retry/reprocessing from DLQ (v2 feature)
- Real-time metrics dashboard (daily aggregation sufficient for MVP)
- Cost optimization or rate limiting
- Async document processing for large files (MVP assumes < 2 MB)

## Decisions

### 1. Upgrade to OpenAI Function Calling (TriageClassificationService)
**Decision:** Replace current JSON parsing approach with GPT-4o function calling using strict schema

**Rationale:**
- Current approach parses JSON response with fallback heuristics; no guarantee of valid enum values
- Function calling enforces schema before returning; invalid responses are caught by OpenAI, not post-processing
- Eliminates hallucination risk (GPT-4o won't return invalid specialty/urgency)
- Parsing is trivial; no custom validation needed

**Current State:**
```csharp
// Current: Basic chat completions + JSON parsing + validation
var response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
var responseText = response.Value.Choices[0].Message.Content;
var triageResponse = ParseAIResponse(responseText); // Fragile JSON parsing
```

**Target State:**
```csharp
// New: Function calling with strict schema
var functions = new[] { new ChatCompletionFunction { Name = "triage_referral", ... } };
var response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
var toolCall = response.Value.ToolCalls[0] as ChatCompletionFunctionToolCall;
var triageResponse = JsonSerializer.Deserialize<TriageResponse>(toolCall.Arguments);
// Arguments are guaranteed to match schema
```

---

### 2. Retry Logic with Exponential Backoff
**Decision:** Max 2 retries for Document Intelligence and OpenAI calls; exponential backoff (1s, 2s)

**Rationale:**
- Transient failures (network, rate limiting) are common in cloud APIs
- 2 retries balances fault tolerance vs. latency (total max ~3 seconds additional per failure)
- Exponential backoff reduces thundering herd on service recovery
- Sequential calls mean one failure fails the whole referral → retry entire sequence

**Implementation in TriageProcessingService:**
```csharp
var extractedText = await RetryAsync(
  () => _documentExtractionService.ExtractTextFromDocumentAsync(blobPath, documentFormat),
  maxRetries: 2
);

if (extractedText == null)
{
  await EmitToDeadLetterAsync(referralId, "extraction_failed");
  return;
}

var triageResponse = await RetryAsync(
  () => _triageClassificationService.ClassifyReferralAsync(triageRequest),
  maxRetries: 2
);

if (triageResponse == null)
{
  await EmitToDeadLetterAsync(referralId, "classification_failed");
  return;
}
```

---

### 3. Dead-Letter Queue for Failures
**Decision:** Azure Storage Queue (not Service Bus) for DLQ; manual review process in v2

**Rationale:**
- Storage Queue is simpler, cheaper, sufficient for MVP
- Stores failed referral ID + error details for async manual inspection
- No automatic reprocessing (operators decide retry manually)
- Clear audit trail for failed cases

**DLQ Message Format:**
```json
{
  "referralId": "guid",
  "failureReason": "document_extraction_failed|classification_failed",
  "errorMessage": "...",
  "timestamp": "2026-03-20T...",
  "retryCount": 2
}
```

---

### 4. Configuration & Secrets Management
**Decision:** local.settings.json for development; Key Vault references for production (Azure Functions binding)

**Rationale:**
- Keeps development config in repo without secrets
- Production uses `@Microsoft.KeyVault(SecretUri=https://...)` binding for automatic injection
- Follows Azure Functions best practices
- Secrets never committed to git

**Config Schema:**
```json
{
  "Values": {
    "ReferralTriageSettings:BlobIncomingPath": "incoming",
    "ReferralTriageSettings:DocumentIntelligenceEndpoint": "https://...",
    "ReferralTriageSettings:DocumentIntelligenceKey": "***",
    "ReferralTriageSettings:AzureOpenAiEndpoint": "https://...",
    "ReferralTriageSettings:AzureOpenAiKey": "***",
    "ReferralTriageSettings:AzureOpenAiDeploymentName": "gpt-4",
    "MetricsAggregationSchedule": "0 0 2 * * *",
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;..."
  },
  "ConnectionStrings": {
    "BlobStorage": "DefaultEndpointsProtocol=https;...",
    "SqlServer": "Server=...;Database=..."
  }
}
```

---

### 5. Test Strategy
**Decision:** Unit tests for services + integration tests for end-to-end flow

**Unit Tests:**
- ValidationService (request/record validation with edge cases)
- DocumentExtractionService (mock Document Intelligence)
- TriageClassificationService (mock OpenAI function calling)

**Integration Tests:**
- End-to-end: intake → extraction (mock) → classification (mock) → persistence
- Retry behavior: verify 2 attempts on transient failure
- DLQ behavior: verify failed referrals are emitted with correct details

**Manual/Smoke Tests:**
- Local: POST to `/api/referrals/intake` with test document
- Verify Blob creation, SQL Referral record, status transitions
- Trigger metrics aggregation manually and verify DailyMetrics

---

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| **Function calling response parsing fails** | Function schema is enforced by OpenAI; only valid responses are returned. Fallback: catch exception and mark as failed + DLQ. |
| **Retry exhaustion on non-transient errors** | 2 retries won't help if auth key is wrong. Mitigate: log error details, make DLQ inspection actionable for ops. |
| **DLQ backlog without visibility** | No auto-retry from DLQ in MVP. Mitigate: Set up alerts on queue depth; document manual inspection process. |
| **Configuration drift between dev/prod** | local.settings.json differs from Key Vault secrets. Mitigate: Document all config keys; automate Key Vault setup in deployment script. |
| **Test coverage gaps** | Easy to miss edge cases in retry logic or DLQ handling. Mitigate: Add integration tests for failure scenarios. |
| **Latency from sequential calls** | Extract + classify happens sequentially, not parallel. Acceptable for MVP (no real-time SLA). |

---

## Migration Plan

1. **Phase 1 - Upgrade TriageClassificationService to Function Calling (2-3 hours)**
   - Refactor TriageClassificationService: replace GetChatCompletionsAsync with function calling
   - Define function schema, update system prompt
   - Parse function call result (ChatCompletionFunctionToolCall)
   - Test: call new service with mock OpenAI; verify output matches TriageResponse schema

2. **Phase 2 - Add Retry Logic with DLQ (3-4 hours)**
   - Create RetryHelper utility (generic retry with exponential backoff)
   - Add Storage Queue client to DI container; register DLQ queue
   - Refactor TriageProcessingService: wrap extraction + classification calls in RetryAsync
   - Add EmitToDeadLetterAsync method; integrate into error paths
   - Test: unit tests for retry behavior; integration test for DLQ routing

3. **Phase 3 - Update TriageProcessorFunction (1-2 hours)**
   - Integrate retry logic and DLQ error handling
   - Enhance logging: log retry attempts, DLQ emissions
   - Handle BlobTrigger errors gracefully (don't re-trigger on transient failures)

4. **Phase 4 - Test Suite (4-5 hours)**
   - Unit tests: ValidationService (edge cases), DocumentExtractionService (mock), TriageClassificationService (function calling)
   - Integration tests: end-to-end intake → extraction → classification → persistence with mocks
   - Retry scenario test: simulate transient failure, verify 2 attempts, then DLQ emission
   - Normal path test: happy path through full pipeline

5. **Phase 5 - Configuration & Documentation (2-3 hours)**
   - Document local.settings.json schema (all keys and sample values)
   - Create Key Vault secret injection sample in Bicep
   - Update README with setup instructions
   - Create runbook for inspecting DLQ, manual retry process

6. **Phase 6 - Local Smoke Test (1-2 hours)**
   - Set up Azurite (local Blob), local SQL Server
   - Run `func start` and POST test referral
   - Verify: Blob creation → DB persistence → status transitions
   - Manually trigger metrics aggregation
   - Verify: DailyMetrics created with correct counts

**Total Estimate: 13-19 hours** (can parallelize some tasks)

---

## Open Questions

1. **OpenAI function calling schema**: Should confidence_score be required, or optional with default? (Current design: optional, defaults to 0.5)

2. **DLQ inspection workflow**: How will ops teams review failed referrals? Manual query of queue, or integrate with monitoring dashboard? (Defer to v2)

3. **Retry backoff timing**: Should exponential backoff be (1s, 2s) or different? (Current proposal: conservative backoff to avoid overwhelming services)
