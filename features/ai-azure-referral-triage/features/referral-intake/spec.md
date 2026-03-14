# Feature: referral-intake

**Location**: `features/referral-intake/spec.md`

---

## 🔗 SpecForge Chain Position
**This is artefact 3 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → **Feature** → Scenario → Test → Plan → Tasks  
                                   ↑ You are here

---

## Implements Requirements
- REQ-001 (from `../../requirements/REQ-001.md`)

---

## Behavior
- REQ-001: When a clinical operations coordinator submits a referral document via HTTP POST, the Referral aggregate shall validate that the ReferralDocument has a supported format (pdf, text, image), size between 1 byte and 50 MB, and non-empty storage path. If valid, Referral shall assign a unique ReferralId, store the document metadata, and emit Referral-Submitted event with ReferralId, DocumentFormat, DocumentHash, and SubmittedAt payload. The submitted Referral aggregate shall then be immediately retrievable from IReferralRepository by ReferralId.

---

## Invariants
When this feature executes, these invariants MUST remain true:
- Referral-UniqueIdInvariant — from Context (enforced by REQ-001)
- Referral-DocumentRequiredInvariant — from Context (enforced by REQ-001)

---

## Domain Events
When this feature executes its requirements, these events are emitted:
- Referral-Submitted — from Context (emitted by REQ-001)

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
Create scenario files in the same directory (`referral-intake/`).

**Scenario file path**: `referral-intake/SCENARIO-001.feature` (sequential: 001, 002, 003...)

**Scenario tags**: Each scenario MUST use @tags matching requirement IDs from Implements Requirements above.

**Coverage rules**:
- Each scenario MUST reference ≥1 requirement ID from Implements Requirements section.
- Create only as many scenarios as needed to validate the feature behaviors.

**Validation before proceeding**:
- Verify all REQ-*.md files exist in `/requirements/`
- Verify Context file exists at `/domain/00-context.md`
- Verify all Invariants and Events listed above exist in Context
