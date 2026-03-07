# Task TASK-015 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[RED]`

This task writes a new failing test for validating ReferralDocument value object creation.

---

## SpecForge Contract
This artefact is governed by the SpecForge Contract.

---

## Requirements (Verbatim Copy-Forward)
The following requirements MUST be copied forward exactly as written:

- REQ-001: When a referral document is submitted via HTTP POST to the referral intake endpoint, the Referral aggregate shall validate file type (PDF, text, or scanned image), validate content size does not exceed 5MB, accept the valid document, store the raw ReferralDocument in Blob Storage at /referrals/incoming/{referralId}, assign a unique ReferralId, emit ReferralReceived domain event with required payload fields, and return the ReferralId to the HTTP caller.

---

## Scenarios and Tests Covered
**Scenarios covered** (from Plan):
- SCENARIO-001, SCENARIO-002

**Test cases this task MUST satisfy**:
- TEST-001 — from ../tests/referral-intake/TEST-001.md
- TEST-002 — from ../tests/referral-intake/TEST-002.md

---

## Task Description

Test: TEST-001 → Requirement: REQ-001 → Result: Referral aggregate must store ReferralDocument value object with FileType attribute matching submitted file type (PDF) and ContentData size matching submitted file size → Approach: Write test that verifies Referral aggregate's ReferralDocument.FileType equals "PDF" and ReferralDocument.ContentData.size equals submitted file length

Test: TEST-002 → Requirement: REQ-001 → Result: Referral aggregate must store ReferralDocument value object with FileType attribute matching submitted file type (TEXT) and ContentData size matching submitted file size → Approach: Write test that verifies Referral aggregate's ReferralDocument.FileType equals "TEXT" and ReferralDocument.ContentData.size equals submitted file length

---

## Commit Instruction
Commit message format (REQUIRED):
```
test(referral-intake): [RED] Validate ReferralDocument value object creation
```

After this commit, the test becomes a static contract. No modifications allowed.

---

## Success Criteria
- Test files created for both TEST-001 and TEST-002 covering ReferralDocument validation
- Test assertion syntax: ReferralDocument.FileType matches file type, ReferralDocument.ContentData size matches file size
- Tests MUST fail before TASK-016 implementation
