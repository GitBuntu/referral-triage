@REQ-002 @REQ-003
Feature: ai-triage-processor

---

## 🔗 SpecForge Chain Position
**This is artefact 4 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → Feature → **Scenario** → Test → Plan → Tasks  
                                      ↑ You are here

---

Scenario: SCENARIO-001 — AI processor classifies referral with specialty and urgency

  Given a valid referral document has been submitted and Referral-Submitted event is emitted
  When the AI triage processor extracts text from the ReferralDocument via OCR if needed
  And sends the extracted text to an AI model
  And the TriageRecord aggregate validates that Specialty is in {cardiology, orthopaedics, neurology, dermatology, general_medicine}
  And validates that Urgency is in {routine, soon, urgent}
  And validates that ExtractedFields contains all required keys (patient_name, dob, symptoms, duration, red_flags) with non-empty string values
  And validates that ClinicalSummary is a non-empty string with length less than 500 characters
  Then the TriageRecord aggregate stores the AI classification results
  And Referral-Triaged event is emitted with ReferralId, Specialty, Urgency, ExtractedFields, ClinicalSummary, and TriagedAt payload
  And the TriageRecord aggregate becomes immediately retrievable from ITriageRecordRepository by specialty
  And the TriageRecord aggregate becomes immediately retrievable from ITriageRecordRepository by urgency
  And the TriageRecord aggregate becomes immediately retrievable from ITriageRecordRepository by ReferralId

---

## Covered Requirements
This scenario validates the following requirement(s) through its Given/When/Then steps.

- REQ-002
- REQ-003

---

## Domain Context
This scenario demonstrates:
- **Domain events that MUST be emitted**: Referral-Triaged
- **Invariants that MUST remain satisfied**: TriageRecord-SpecialtyInvariant, TriageRecord-UrgencyInvariant, TriageRecord-KeyFieldsInvariant, TriageRecord-ClinicalSummaryInvariant

---

## SpecForge Rules
- Must reference ≥1 requirement via tags and Covered Requirements.
- Feature tags MUST exactly match Covered Requirements IDs (in same order).
- Must NOT introduce new invariants, events, or domain concepts not in the Context.
- Must demonstrate that domain events are emitted and invariants are preserved.
- All Given/When/Then text MUST be copied verbatim from the requirement — never paraphrase or abbreviate.

---

## Next Step Directive
Create a test file named `SCENARIO-001-TEST-001.md` under `/tests/ai-triage-processor/`.

The test MUST reference all requirement IDs covered by this scenario.
