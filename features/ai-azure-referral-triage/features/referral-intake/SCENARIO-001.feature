@REQ-001
Feature: referral-intake

---

## 🔗 SpecForge Chain Position
**This is artefact 4 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → Feature → **Scenario** → Test → Plan → Tasks  
                                      ↑ You are here

---

Scenario: SCENARIO-001 — HTTP endpoint accepts and validates referral document

  Given a clinical operations coordinator submits a referral document via HTTP POST with format=pdf, size between 1 byte and 50 MB, and non-empty storage path
  When the Referral aggregate validates the ReferralDocument
  Then the Referral aggregate assigns a unique ReferralId
  And a Referral aggregate is created with the validated document metadata
  And Referral-Submitted event is emitted with ReferralId, DocumentFormat, DocumentHash, and SubmittedAt payload
  And the Referral aggregate becomes immediately retrievable from IReferralRepository by ReferralId

---

## Covered Requirements
This scenario validates the following requirement(s) through its Given/When/Then steps.

- REQ-001

---

## Domain Context
This scenario demonstrates:
- **Domain events that MUST be emitted**: Referral-Submitted
- **Invariants that MUST remain satisfied**: Referral-UniqueIdInvariant, Referral-DocumentRequiredInvariant

---

## SpecForge Rules
- Must reference ≥1 requirement via tags and Covered Requirements.
- Feature tags MUST exactly match Covered Requirements IDs (in same order).
- Must NOT introduce new invariants, events, or domain concepts not in the Context.
- Must demonstrate that domain events are emitted and invariants are preserved.
- All Given/When/Then text MUST be copied verbatim from the requirement — never paraphrase or abbreviate.

---

## Next Step Directive
Create a test file named `SCENARIO-001-TEST-001.md` under `/tests/referral-intake/`.

The test MUST reference all requirement IDs covered by this scenario.
