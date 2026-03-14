# Implementation Plan for ai-triage-processor

---

## 🔗 SpecForge Chain Position
**This is artefact 6 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → Task

You are defining: Plan.

---

# Requirements (Verbatim Copy-Forward)
The following requirements MUST be copied forward exactly as written from source files:

- REQ-002: When a valid referral document has been submitted and the Referral-Submitted event is emitted, the AI triage processor shall extract text from the ReferralDocument (using OCR if the document is scanned/image format), send the extracted text to an AI model, and receive back a structured classification result. The TriageRecord aggregate shall then accept the AI result, validate that Specialty is in the allowed set {cardiology, orthopaedics, neurology, dermatology, general_medicine}, and preserve the TriageRecord-SpecialtyInvariant. The TriageRecord shall then be immediately retrievable from ITriageRecordRepository by specialty.
- REQ-003: When an AI model generates triage classification results, the TriageRecord aggregate shall validate that Urgency is one of {routine, soon, urgent}, that ExtractedFields contains all required keys (patient_name, dob, symptoms, duration, red_flags) with non-empty string values, and that ClinicalSummary is a non-empty string with length less than 500 characters. If all validations pass, the TriageRecord shall emit Referral-Triaged event with all triage details in the payload, and the TriageRecord shall be immediately retrievable from ITriageRecordRepository by urgency and by ReferralId.

---

# Scenarios and Tests
List all Scenarios and Tests for this feature IN THE EXACT ORDER listed:

- Scenario: SCENARIO-001.feature → Tests: TEST-002

---

# Test-Driven Planning
Each planning step MUST be derived directly from ONE specific assertion in TEST-002.md files.

**Step Format**: `Step N: [AGGREGATE_NAME] must [EXACT_ASSERTION_SYNTAX_from_test] → Tests {{TEST_ID_X}} → Validates {{REQ_ID_Y}}`

- Step 1: TriageRecord must Specialty in {cardiology, orthopaedics, neurology, dermatology, general_medicine} → Tests TEST-002 → Validates REQ-002
- Step 2: TriageRecord must Urgency in {routine, soon, urgent} → Tests TEST-002 → Validates REQ-003
- Step 3: TriageRecord must ExtractedFields contains {patient_name, dob, symptoms, duration, red_flags} non-empty → Tests TEST-002 → Validates REQ-003
- Step 4: TriageRecord must ClinicalSummary != empty AND length < 500 → Tests TEST-002 → Validates REQ-003
- Step 5: TriageRecord must OCR text extraction occurred for scanned/image documents → Tests TEST-002 → Validates REQ-002
- Step 6: TriageRecord must AI model received extracted text as input → Tests TEST-002 → Validates REQ-002
- Step 7: TriageRecord must TriageRecord-SpecialtyInvariant is preserved → Tests TEST-002 → Validates REQ-002
- Step 8: TriageRecord must TriageRecord-UrgencyInvariant is preserved → Tests TEST-002 → Validates REQ-003
- Step 9: TriageRecord must TriageRecord-KeyFieldsInvariant is preserved → Tests TEST-002 → Validates REQ-003
- Step 10: TriageRecord must TriageRecord-ClinicalSummaryInvariant is preserved → Tests TEST-002 → Validates REQ-003
- Step 11: TriageRecord must Referral-Triaged emitted == true → Tests TEST-002 → Validates REQ-003
- Step 12: TriageRecord must Referral-Triaged.ReferralId == referral_id → Tests TEST-002 → Validates REQ-003
- Step 13: TriageRecord must Referral-Triaged.Specialty == assigned_specialty → Tests TEST-002 → Validates REQ-003
- Step 14: TriageRecord must Referral-Triaged.Urgency == assigned_urgency → Tests TEST-002 → Validates REQ-003
- Step 15: TriageRecord must Referral-Triaged.ExtractedFields contains required keys → Tests TEST-002 → Validates REQ-003
- Step 16: TriageRecord must Referral-Triaged.ClinicalSummary == summary_text → Tests TEST-002 → Validates REQ-003
- Step 17: TriageRecord must Referral-Triaged.TriagedAt == current_timestamp → Tests TEST-002 → Validates REQ-003
- Step 18: TriageRecord must retrievable from ITriageRecordRepository.LoadBySpecialty(specialty) → Tests TEST-002 → Validates REQ-002
- Step 19: TriageRecord must retrievable from ITriageRecordRepository.LoadByUrgency(urgency) → Tests TEST-002 → Validates REQ-003
- Step 20: TriageRecord must retrievable from ITriageRecordRepository.Load(ReferralId) → Tests TEST-002 → Validates REQ-003

---

# Task Index

This list defines ALL tasks for this ENTIRE FEATURE in TDD alternating red-green cycles.

- TASK-001: Validate specialty is in allowed set — [RED] — NOT STARTED
- TASK-002: Validate specialty is in allowed set — [IMPL] — NOT STARTED
- TASK-003: Validate urgency is in allowed set — [RED] — NOT STARTED
- TASK-004: Validate urgency is in allowed set — [IMPL] — NOT STARTED
- TASK-005: Validate extracted fields contain all required keys non-empty — [RED] — NOT STARTED
- TASK-006: Validate extracted fields contain all required keys non-empty — [IMPL] — NOT STARTED
- TASK-007: Validate clinical summary is non-empty and under 500 chars — [RED] — NOT STARTED
- TASK-008: Validate clinical summary is non-empty and under 500 chars — [IMPL] — NOT STARTED
- TASK-009: Extract text from document via OCR if needed — [RED] — NOT STARTED
- TASK-010: Extract text from document via OCR if needed — [IMPL] — NOT STARTED
- TASK-011: Send extracted text to AI model for classification — [RED] — NOT STARTED
- TASK-012: Send extracted text to AI model for classification — [IMPL] — NOT STARTED
- TASK-013: Preserve TriageRecord-SpecialtyInvariant on triage completion — [RED] — NOT STARTED
- TASK-014: Preserve TriageRecord-SpecialtyInvariant on triage completion — [IMPL] — NOT STARTED
- TASK-015: Preserve TriageRecord-UrgencyInvariant on triage completion — [RED] — NOT STARTED
- TASK-016: Preserve TriageRecord-UrgencyInvariant on triage completion — [IMPL] — NOT STARTED
- TASK-017: Preserve TriageRecord-KeyFieldsInvariant on triage completion — [RED] — NOT STARTED
- TASK-018: Preserve TriageRecord-KeyFieldsInvariant on triage completion — [IMPL] — NOT STARTED
- TASK-019: Preserve TriageRecord-ClinicalSummaryInvariant on triage completion — [RED] — NOT STARTED
- TASK-020: Preserve TriageRecord-ClinicalSummaryInvariant on triage completion — [IMPL] — NOT STARTED
- TASK-021: Emit Referral-Triaged event with all required payload fields — [RED] — NOT STARTED
- TASK-022: Emit Referral-Triaged event with all required payload fields — [IMPL] — NOT STARTED
- TASK-023: TriageRecord retrievable by specialty from repository — [RED] — NOT STARTED
- TASK-024: TriageRecord retrievable by specialty from repository — [IMPL] — NOT STARTED
- TASK-025: TriageRecord retrievable by urgency from repository — [RED] — NOT STARTED
- TASK-026: TriageRecord retrievable by urgency from repository — [IMPL] — NOT STARTED
- TASK-027: TriageRecord retrievable by ReferralId from repository — [RED] — NOT STARTED
- TASK-028: TriageRecord retrievable by ReferralId from repository — [IMPL] — NOT STARTED

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
