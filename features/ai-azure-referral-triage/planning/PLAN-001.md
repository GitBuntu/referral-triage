# Implementation Plan for Referral Intake

---

## 🔗 SpecForge Chain Position
**This is artefact 6 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → Feature → Scenario → Test → **Plan** → Tasks  
                                                                  ↑ You are here

---

## SpecForge Contract
This artefact is governed by the SpecForge Contract. Strict enforcement rules:

1. Do NOT reinterpret any requirement. COPY Requirement Statement sections verbatim.
2. Do NOT summarise or simplify requirement text. COPY complete text as written.
3. Do NOT infer missing behaviour. COPY only what exists in requirements, scenarios, tests.
4. Do NOT add assumptions. REFERENCE only assertions explicitly defined in test files.
5. Do NOT remove constraints. INCLUDE all invariants and events from requirements.
6. Do NOT introduce new invariants, domain events, or states. Copy only from Context and requirements.
7. Do NOT use alternative formatting for planning steps. Use ONLY the format specified in Test-Driven Planning section.
8. Do NOT invent task names or reorder tasks. Copy EXACT order from Task Index.

---

## Requirements (Verbatim Copy-Forward)
The following requirements MUST be copied forward exactly as written from source files:

- REQ-001: When a referral document is submitted via HTTP POST to the referral intake endpoint, the Referral aggregate shall validate file type (PDF, text, or scanned image), validate content size does not exceed 5MB, accept the valid document, store the raw ReferralDocument in Blob Storage at /referrals/incoming/{referralId}, assign a unique ReferralId, emit ReferralReceived domain event with required payload fields, and return the ReferralId to the HTTP caller.

---

## Scenarios and Tests
List all Scenarios and Tests for this feature IN THE EXACT ORDER listed in ../features/referral-intake/spec.md:

- Scenario: SCENARIO-001.feature → Tests: TEST-001
- Scenario: SCENARIO-002.feature → Tests: TEST-002
- Scenario: SCENARIO-003.feature → Tests: TEST-003
- Scenario: SCENARIO-004.feature → Tests: TEST-004
- Scenario: SCENARIO-005.feature → Tests: TEST-005

---

## Test-Driven Planning
Each planning step MUST be derived directly from specific test assertions in test files.  
**No implementation decisions, no design choices, no architecture assumptions. ONLY decomposition of test assertions into discrete executable steps.**

**STEP FORMAT (REQUIRED - use EXACTLY this structure):**
```
Step N: [AGGREGATE_NAME] must [EXACT_ASSERTION_SYNTAX_from_test] → Tests {{TEST_ID_X}} → Validates {{REQ_ID_Y}}
```

---

Step 1: Referral aggregate must reject documents with unsupported file type and return HTTP 400 with "Unsupported file type" error message → Tests TEST-003 → Validates REQ-001

Step 2: Referral aggregate must reject documents exceeding 5 MB and return HTTP 413 with "Request entity too large" error message → Tests TEST-004 → Validates REQ-001

Step 3: Referral aggregate must reject empty documents (0 bytes) and return HTTP 400 with "Document is empty" error message → Tests TEST-005 → Validates REQ-001

Step 4: Referral aggregate must accept valid PDF documents, assign a unique ReferralId (UUID format), and return HTTP 201 with referralId and timestamp in response body → Tests TEST-001 → Validates REQ-001

Step 5: Referral aggregate must accept valid TEXT documents, assign a unique ReferralId (UUID format), and return HTTP 201 with referralId and timestamp in response body → Tests TEST-002 → Validates REQ-001

Step 6: ReferralDocument must be stored in Blob Storage at exact path `/referrals/incoming/{referralId}` with content identical to submitted file (byte-for-byte comparison for binary, character-for-character for text) → Tests TEST-001, TEST-002 → Validates REQ-001

Step 7: Referral aggregate must emit ReferralReceived domain event with payload containing ReferralId (UUID), DocumentId (UUID), FileType (matching submitted type), UploadTimestamp (RFC3339 datetime), and UploadSource ("/referrals/intake") exactly once when document is accepted → Tests TEST-001, TEST-002 → Validates REQ-001

Step 8: Referral aggregate must store ReferralDocument value object with FileType attribute matching submitted file type (PDF or TEXT) and ContentData size matching submitted file size → Tests TEST-001, TEST-002 → Validates REQ-001

---

## SpecForge Rules
- **MUST** be derived directly from test assertions in test files (sections: Invariant Assertions, Domain Event Assertions, Outcome Assertions)
- **MUST NOT** introduce new requirements, invariants, domain events, or states — only copy from requirements and tests
- **Each planning step MUST** be traceable back to:
  - A {{TEST_ID_X}}.md file (in "Tests" column)
  - A {{REQ_ID_X}}.md file (in "Validates" column)
  - An exact assertion from test file
- **Planning steps MUST be granular**: Maximum one aggregate per step. If multiple aggregates, create separate steps.
- **Planning steps MUST be sequential**: Numbered 1, 2, 3, 4, ... in order of appearance (first step in list = Step 1)
- **Planning steps MUST match test order**: Sequence of planning steps should follow sequence of test cases
- **ZERO coverage gaps**: Must have planning step for EVERY test case listed in Scenarios and Tests section
- **No vague verbs**: Use ONLY assertion syntax from tests, never generic implementation words
- **No assumptions**: Do NOT add missing steps, infer behavior, or assume requirements beyond what is written
- **All referenced files MUST exist**: Verify /requirements/REQ-001.md, /features/referral-intake/SCENARIO-*.feature, /tests/referral-intake/TEST-*.md before saving this plan

---

## Task Index

This list defines ALL tasks for this feature in TDD alternating red-green cycles. The LLM MUST NOT add, remove, reorder, or rename tasks.

**TDD Pairing Rule (NON-NEGOTIABLE)**:
- Tasks MUST alternate: [RED] → [IMPL] → [RED] → [IMPL] → [RED] → [IMPL] → [RED] → [IMPL]
- Each [RED] task (test writing) is immediately followed by [IMPL] task (implementation)
- TASK-001 must be [RED], TASK-002 must be [IMPL], TASK-003 must be [RED], TASK-004 must be [IMPL], etc.
- Do NOT create consecutive [RED] tasks or consecutive [IMPL] tasks

Each task MUST use format: `- TASK-NNN: [BRIEF TITLE] — [PHASE] — [EXACT_STATUS]`

- TASK-001: Reject unsupported file types — [RED] — NOT-STARTED
- TASK-002: Implement file type validation — [IMPL] — NOT-STARTED
- TASK-003: Reject oversized documents (>5MB) — [RED] — NOT-STARTED
- TASK-004: Implement file size validation (max) — [IMPL] — NOT-STARTED
- TASK-005: Reject empty documents (0 bytes) — [RED] — NOT-STARTED
- TASK-006: Implement file size validation (min) — [IMPL] — NOT-STARTED
- TASK-007: Accept valid PDF and return ReferralId — [RED] — NOT-STARTED
- TASK-008: Implement PDF intake, ReferralId assignment, and HTTP response — [IMPL] — NOT-STARTED
- TASK-009: Accept valid TEXT and return ReferralId — [RED] — NOT-STARTED
- TASK-010: Implement TEXT intake and HTTP response — [IMPL] — NOT-STARTED
- TASK-011: Store ReferralDocument in Blob Storage — [RED] — NOT-STARTED
- TASK-012: Implement Blob Storage persistence — [IMPL] — NOT-STARTED
- TASK-013: Emit ReferralReceived domain event with payload — [RED] — NOT-STARTED
- TASK-014: Implement domain event emission — [IMPL] — NOT-STARTED
- TASK-015: Validate ReferralDocument value object creation — [RED] — NOT-STARTED
- TASK-016: Implement ReferralDocument value object validation — [IMPL] — NOT-STARTED
