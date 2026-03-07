# Test TEST-001

---

## 🔗 SpecForge Chain Position
**This is artefact 5 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → Feature → Scenario → **Test** → Plan → Tasks  
                                                      ↑ You are here

---

## Requirement Traceability
**Primary Requirement**: REQ-001 (the requirement this test MOST DIRECTLY validates)

**All Referenced Requirements** (direct references only; do NOT invent transitive dependencies):
- REQ-001 — Primary requirement (directly tested)

**Originating Scenario**: SCENARIO-001.feature (from `../../features/referral-intake/SCENARIO-001.feature`)

---

## Assertions
**CRITICAL RULE: This section contains assertions in STRUCTURED format only. Each assertion MUST be one of the 4 patterns below. Zero narrative prose allowed between assertions.**

**Invariant Assertions** (checked AFTER SCENARIO-001.feature has been implemented and executed):
- Assert: Referral-UniqueIdInvariant is preserved
  - Context invariant name: Referral-UniqueIdInvariant
  - Assertion: Assert: Each Referral.ReferralId must be globally unique (no two Referral aggregates share the same ReferralId)
- Assert: Referral-DocumentRequiredInvariant is preserved
  - Context invariant name: Referral-DocumentRequiredInvariant
  - Assertion: Assert: Referral.Document != null

**Domain Event Assertions** (checked when SCENARIO-001.feature When/Then steps are executed AFTER implementation):
- Assert: Referral-Submitted is emitted = true
- Assert: Referral-Submitted.ReferralId == generated_uuid
- Assert: Referral-Submitted.DocumentFormat == pdf
- Assert: Referral-Submitted.DocumentHash == valid_sha256_hash
- Assert: Referral-Submitted.SubmittedAt == current_timestamp

**Required Outcome Assertions** (directly from REQ-001 Requirement Statement):
- Assert: REQ-001 requirement is satisfied
  - Requirement statement text: When a clinical operations coordinator submits a referral document via HTTP POST, the Referral aggregate shall validate that the ReferralDocument has a supported format (pdf, text, image), size between 1 byte and 50 MB, and non-empty storage path. If valid, Referral shall assign a unique ReferralId, store the document metadata, and emit Referral-Submitted event with ReferralId, DocumentFormat, DocumentHash, and SubmittedAt payload. The submitted Referral aggregate shall then be immediately retrievable from IReferralRepository by ReferralId.
  - Assertion syntax:
    - Assert: ReferralDocument.Format in {pdf, text, image}
    - Assert: ReferralDocument.Size > 0 AND ReferralDocument.Size < 52428800 (50 MB in bytes)
    - Assert: ReferralDocument.StoragePath != empty
    - Assert: Referral.ReferralId != null AND Referral.ReferralId is UUID
    - Assert: Referral is retrievable from IReferralRepository.Load(ReferralId)

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
- Verify `/requirements/REQ-001.md` exists
- Verify `/features/referral-intake/SCENARIO-001.feature` exists
- Verify all invariant names match exactly from Context
- Verify all event names match exactly from Context
- Verify zero narrative sentences exist in Assertions section (only assertion syntax)
