# Test TEST-002

---

## 🔗 SpecForge Chain Position
**This is artefact 5 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → Feature → Scenario → **Test** → Plan → Tasks  
                                                      ↑ You are here

---

## Requirement Traceability
**Primary Requirement**: REQ-002 (the requirement this test MOST DIRECTLY validates)

**All Referenced Requirements** (direct references only; do NOT invent transitive dependencies):
- REQ-002 — Primary requirement (directly tested)
- REQ-003 — Additional requirement this test validates (directly tested)

**Originating Scenario**: SCENARIO-001.feature (from `../../features/ai-triage-processor/SCENARIO-001.feature`)

---

## Assertions
**CRITICAL RULE: This section contains assertions in STRUCTURED format only. Each assertion MUST be one of the 4 patterns below. Zero narrative prose allowed between assertions.**

**Invariant Assertions** (checked AFTER SCENARIO-001.feature has been implemented and executed):
- Assert: TriageRecord-SpecialtyInvariant is preserved
  - Context invariant name: TriageRecord-SpecialtyInvariant
  - Assertion: Assert: TriageRecord.TriageResult.Specialty in {cardiology, orthopaedics, neurology, dermatology, general_medicine}
- Assert: TriageRecord-UrgencyInvariant is preserved
  - Context invariant name: TriageRecord-UrgencyInvariant
  - Assertion: Assert: TriageRecord.TriageResult.Urgency in {routine, soon, urgent}
- Assert: TriageRecord-KeyFieldsInvariant is preserved
  - Context invariant name: TriageRecord-KeyFieldsInvariant
  - Assertion: Assert: TriageRecord.TriageResult.ExtractedFields contains {patient_name, dob, symptoms, duration, red_flags} with non-empty values
- Assert: TriageRecord-ClinicalSummaryInvariant is preserved
  - Context invariant name: TriageRecord-ClinicalSummaryInvariant
  - Assertion: Assert: TriageRecord.TriageResult.ClinicalSummary != empty AND length < 500

**Domain Event Assertions** (checked when SCENARIO-001.feature When/Then steps are executed AFTER implementation):
- Assert: Referral-Triaged is emitted = true
- Assert: Referral-Triaged.ReferralId == referral_id
- Assert: Referral-Triaged.Specialty == cardiology
- Assert: Referral-Triaged.Urgency == urgent
- Assert: Referral-Triaged.ExtractedFields contains {patient_name, dob, symptoms, duration, red_flags}
- Assert: Referral-Triaged.ClinicalSummary == summary_text
- Assert: Referral-Triaged.TriagedAt == current_timestamp

**Required Outcome Assertions** (directly from REQ-002 and REQ-003 Requirement Statements):
- Assert: REQ-002 requirement is satisfied
  - Requirement statement text: When a valid referral document has been submitted and the Referral-Submitted event is emitted, the AI triage processor shall extract text from the ReferralDocument (using OCR if the document is scanned/image format), send the extracted text to an AI model, and receive back a structured classification result. The TriageRecord aggregate shall then accept the AI result, validate that Specialty is in the allowed set {cardiology, orthopaedics, neurology, dermatology, general_medicine}, and preserve the TriageRecord-SpecialtyInvariant. The TriageRecord shall then be immediately retrievable from ITriageRecordRepository by specialty.
  - Assertion syntax:
    - Assert: OCR text extraction occurred for scanned/image documents
    - Assert: AI model received extracted text as input
    - Assert: TriageRecord.TriageResult.Specialty in {cardiology, orthopaedics, neurology, dermatology, general_medicine}
    - Assert: TriageRecord is retrievable from ITriageRecordRepository.LoadBySpecialty(specialty)
- Assert: REQ-003 requirement is satisfied
  - Requirement statement text: When an AI model generates triage classification results, the TriageRecord aggregate shall validate that Urgency is in {routine, soon, urgent}, that ExtractedFields contains all required keys (patient_name, dob, symptoms, duration, red_flags) with non-empty string values, and that ClinicalSummary is a non-empty string with length less than 500 characters. If all validations pass, the TriageRecord shall emit Referral-Triaged event with all triage details in the payload, and the TriageRecord shall be immediately retrievable from ITriageRecordRepository by urgency and by ReferralId.
  - Assertion syntax:
    - Assert: TriageRecord.TriageResult.Urgency in {routine, soon, urgent}
    - Assert: ExtractedFields keys {patient_name, dob, symptoms, duration, red_flags} all present and non-empty
    - Assert: ClinicalSummary != empty AND length < 500
    - Assert: TriageRecord is retrievable from ITriageRecordRepository.LoadByUrgency(urgency)
    - Assert: TriageRecord is retrievable from ITriageRecordRepository.Load(ReferralId)

---

## SpecForge Rules
- MUST reference ≥1 requirement via Primary Requirement and All Referenced Requirements section.
- MUST link to originating SCENARIO-001.feature.
- MUST assert ALL invariants from SCENARIO-001.feature Domain Context section.
- MUST assert ALL domain events from SCENARIO-001.feature Domain Context section.
- MUST NOT introduce new invariants or domain events — only assert those from Context.
- ZERO narrative sentences allowed — only assertion syntax.

---

## Next Step Directive
Create one plan file named `/planning/PLAN-001.md` using the `{{PLAN_ID}}.template.md` template.

The plan MUST decompose ALL assertions from this test into executable planning steps.

Validate test-to-plan chain before proceeding:
- Verify `/requirements/REQ-002.md` and `/requirements/REQ-003.md` exist
- Verify `/features/ai-triage-processor/SCENARIO-001.feature` exists
- Verify all invariant names match exactly from Context
- Verify all event names match exactly from Context
- Verify zero narrative sentences exist in Assertions section (only assertion syntax)
