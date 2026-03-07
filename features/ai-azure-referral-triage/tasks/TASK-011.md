# Task TASK-011 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[RED]`

This task writes a new failing test for storing ReferralDocument in Blob Storage.

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

Test: TEST-001 → Requirement: REQ-001 → Result: ReferralDocument must be stored in Blob Storage at exact path `/referrals/incoming/{referralId}` with binary content identical to submitted file (byte-for-byte comparison for PDF) → Approach: Write test that submits valid PDF, asserts file exists in Blob Storage at `/referrals/incoming/{referralId}`, asserts stored file binary content matches submitted file byte-for-byte

Test: TEST-002 → Requirement: REQ-001 → Result: ReferralDocument must be stored in Blob Storage at exact path `/referrals/incoming/{referralId}` with text content identical to submitted file (character-for-character comparison for text, UTF-8 encoding preserved) → Approach: Write test that submits valid text file, asserts file exists in Blob Storage at `/referrals/incoming/{referralId}`, asserts stored file text content matches submitted file character-for-character with UTF-8 encoding

---

## Commit Instruction
Commit message format (REQUIRED):
```
test(referral-intake): [RED] Store ReferralDocument in Blob Storage
```

After this commit, the test becomes a static contract. No modifications allowed.

---

## Success Criteria
- Test files created for both TEST-001 and TEST-002 covering Blob Storage assertions
- Test assertion syntax: File exists at `/referrals/incoming/{referralId}`, content matches submitted file (byte/character comparison)
- Tests MUST fail before TASK-012 implementation
