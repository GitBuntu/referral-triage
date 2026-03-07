# Implementation Plan for referral-intake

---

## 🔗 SpecForge Chain Position
**This is artefact 6 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → Task

You are defining: Plan.

---

# Requirements (Verbatim Copy-Forward)
The following requirements MUST be copied forward exactly as written from source files:

- REQ-001: When a clinical operations coordinator submits a referral document via HTTP POST, the Referral aggregate shall validate that the ReferralDocument has a supported format (pdf, text, image), size between 1 byte and 50 MB, and non-empty storage path. If valid, Referral shall assign a unique ReferralId, store the document metadata, and emit Referral-Submitted event with ReferralId, DocumentFormat, DocumentHash, and SubmittedAt payload. The submitted Referral aggregate shall then be immediately retrievable from IReferralRepository by ReferralId.

---

# Scenarios and Tests
List all Scenarios and Tests for this feature IN THE EXACT ORDER listed:

- Scenario: SCENARIO-001.feature → Tests: TEST-001

---

# Test-Driven Planning
Each planning step MUST be derived directly from ONE specific assertion in TEST-001.md files.

**Step Format**: `Step N: [AGGREGATE_NAME] must [EXACT_ASSERTION_SYNTAX_from_test] → Tests {{TEST_ID_X}} → Validates {{REQ_ID_Y}}`

- Step 1: Referral must Format in {pdf, text, image} → Tests TEST-001 → Validates REQ-001
- Step 2: Referral must Size > 0 AND Size < 52428800 → Tests TEST-001 → Validates REQ-001
- Step 3: Referral must StoragePath != empty → Tests TEST-001 → Validates REQ-001
- Step 4: Referral must ReferralId != null AND is UUID → Tests TEST-001 → Validates REQ-001
- Step 5: Referral must retrievable from IReferralRepository.Load(ReferralId) → Tests TEST-001 → Validates REQ-001
- Step 6: Referral must Referral-UniqueIdInvariant is preserved → Tests TEST-001 → Validates REQ-001
- Step 7: Referral must Referral-DocumentRequiredInvariant is preserved → Tests TEST-001 → Validates REQ-001
- Step 8: Referral must Referral-Submitted emitted == true → Tests TEST-001 → Validates REQ-001
- Step 9: Referral must Referral-Submitted.ReferralId == generated_uuid → Tests TEST-001 → Validates REQ-001
- Step 10: Referral must Referral-Submitted.DocumentFormat == pdf → Tests TEST-001 → Validates REQ-001
- Step 11: Referral must Referral-Submitted.DocumentHash == valid_sha256_hash → Tests TEST-001 → Validates REQ-001
- Step 12: Referral must Referral-Submitted.SubmittedAt == current_timestamp → Tests TEST-001 → Validates REQ-001

---

# Task Index

This list defines ALL tasks for this ENTIRE FEATURE in TDD alternating red-green cycles.

- TASK-001: Validate document format is pdf, text, or image — [RED] — NOT STARTED
- TASK-002: Validate document format is pdf, text, or image — [IMPL] — NOT STARTED
- TASK-003: Validate document size between 1 byte and 50 MB — [RED] — NOT STARTED
- TASK-004: Validate document size between 1 byte and 50 MB — [IMPL] — NOT STARTED
- TASK-005: Validate storage path is non-empty — [RED] — NOT STARTED
- TASK-006: Validate storage path is non-empty — [IMPL] — NOT STARTED
- TASK-007: Assign unique ReferralId as UUID — [RED] — NOT STARTED
- TASK-008: Assign unique ReferralId as UUID — [IMPL] — NOT STARTED
- TASK-009: Referral aggregate retrievable by ReferralId from repository — [RED] — NOT STARTED
- TASK-010: Referral aggregate retrievable by ReferralId from repository — [IMPL] — NOT STARTED
- TASK-011: Preserve Referral-UniqueIdInvariant after submission — [RED] — NOT STARTED
- TASK-012: Preserve Referral-UniqueIdInvariant after submission — [IMPL] — NOT STARTED
- TASK-013: Preserve Referral-DocumentRequiredInvariant after submission — [RED] — NOT STARTED
- TASK-014: Preserve Referral-DocumentRequiredInvariant after submission — [IMPL] — NOT STARTED
- TASK-015: Emit Referral-Submitted domain event on intake — [RED] — NOT STARTED
- TASK-016: Emit Referral-Submitted domain event on intake — [IMPL] — NOT STARTED
- TASK-017: Referral-Submitted event includes ReferralId in payload — [RED] — NOT STARTED
- TASK-018: Referral-Submitted event includes ReferralId in payload — [IMPL] — NOT STARTED
- TASK-019: Referral-Submitted event includes DocumentFormat in payload — [RED] — NOT STARTED
- TASK-020: Referral-Submitted event includes DocumentFormat in payload — [IMPL] — NOT STARTED
- TASK-021: Referral-Submitted event includes DocumentHash in payload — [RED] — NOT STARTED
- TASK-022: Referral-Submitted event includes DocumentHash in payload — [IMPL] — NOT STARTED
- TASK-023: Referral-Submitted event includes SubmittedAt timestamp in payload — [RED] — NOT STARTED
- TASK-024: Referral-Submitted event includes SubmittedAt timestamp in payload — [IMPL] — NOT STARTED

---

# Resume Logic

To resume work on this plan:

1. Read the Task Index section above
2. Scan status values to find: `MIN(TASK-NNN where STATUS == "NOT STARTED")`
3. Read the PHASE field for that task
   - If [RED]: Create test file, write failing test, commit with `[RED]` marker
   - If [IMPL]: Write implementation code to make paired [RED] test pass
4. Instantiate that task using `/tasks/{{TASK_ID}}.md` template
5. If ALL tasks have status COMPLETE, stop — no more work needed
