# Feature: ai-triage-processor

**Location**: `features/ai-triage-processor/spec.md`

---

## 🔗 SpecForge Chain Position
**This is artefact 3 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → **Feature** → Scenario → Test → Plan → Tasks  
                                   ↑ You are here

---

## Implements Requirements
- REQ-002 (from `../../requirements/REQ-002.md`)
- REQ-003 (from `../../requirements/REQ-003.md`)

---

## Behavior
- REQ-002: When a valid referral document has been submitted and the Referral-Submitted event is emitted, the AI triage processor shall extract text from the ReferralDocument (using OCR if the document is scanned/image format), send the extracted text to an AI model, and receive back a structured classification result. The TriageRecord aggregate shall then accept the AI result, validate that Specialty is in the allowed set {cardiology, orthopaedics, neurology, dermatology, general_medicine}, and preserve the TriageRecord-SpecialtyInvariant. The TriageRecord shall then be immediately retrievable from ITriageRecordRepository by specialty.
- REQ-003: When an AI model generates triage classification results, the TriageRecord aggregate shall validate that Urgency is one of {routine, soon, urgent}, that ExtractedFields contains all required keys (patient_name, dob, symptoms, duration, red_flags) with non-empty string values, and that ClinicalSummary is a non-empty string with length less than 500 characters. If all validations pass, the TriageRecord shall emit Referral-Triaged event with all triage details in the payload, and the TriageRecord shall be immediately retrievable from ITriageRecordRepository by urgency and by ReferralId.

---

## Invariants
When this feature executes, these invariants MUST remain true:
- TriageRecord-SpecialtyInvariant — from Context (enforced by REQ-002)
- TriageRecord-UrgencyInvariant — from Context (enforced by REQ-003)
- TriageRecord-KeyFieldsInvariant — from Context (enforced by REQ-003)
- TriageRecord-ClinicalSummaryInvariant — from Context (enforced by REQ-003)

---

## Domain Events
When this feature executes its requirements, these events are emitted:
- Referral-Triaged — from Context (emitted by REQ-002, REQ-003)

---

## SpecForge Rules
- Must reference one or more requirements in Implements Requirements.
- All referenced requirements must exist in `/requirements`.
- Must not introduce new invariants or events beyond those from the Context.
- All invariants listed here must be supported by at least one requirement.
- All events listed here must be triggered by at least one requirement.
- Scenarios in this feature must use @tags matching the requirement IDs listed here.

---

## Next Step Directive
Create scenario files in the same directory (`ai-triage-processor/`).

**Scenario file path**: `ai-triage-processor/SCENARIO-001.feature` (sequential: 001, 002, 003...)

**Scenario tags**: Each scenario MUST use @tags matching requirement IDs from Implements Requirements above.

**Coverage rules**:
- Each scenario MUST reference ≥1 requirement ID from Implements Requirements section.
- Create only as many scenarios as needed to validate the feature behaviors.

**Validation before proceeding**:
- Verify all REQ-*.md files exist in `/requirements/`
- Verify Context file exists at `/domain/00-context.md`
- Verify all Invariants and Events listed above exist in Context
