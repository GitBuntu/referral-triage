# Task TASK-013 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[RED]`

This task writes a new failing test for emitting ReferralReceived domain event with required payload.

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

Test: TEST-001 → Requirement: REQ-001 → Result: Referral aggregate must emit ReferralReceived domain event with payload containing ReferralId (UUID), DocumentId (UUID), FileType (matching submitted type), UploadTimestamp (RFC3339 datetime), and UploadSource ("/referrals/intake") exactly once when document is accepted → Approach: Write test that submits valid PDF, asserts ReferralReceived event is emitted exactly once with all required payload fields present and non-null

Test: TEST-002 → Requirement: REQ-001 → Result: Referral aggregate must emit ReferralReceived domain event with payload containing ReferralId (UUID), DocumentId (UUID), FileType (matching submitted type), UploadTimestamp (RFC3339 datetime), and UploadSource ("/referrals/intake") exactly once when document is accepted → Approach: Write test that submits valid text file, asserts ReferralReceived event is emitted exactly once with all required payload fields present and non-null

---

## Commit Instruction
Commit message format (REQUIRED):
```
test(referral-intake): [RED] Emit ReferralReceived domain event with payload
```

After this commit, the test becomes a static contract. No modifications allowed.

---

## Success Criteria
- Test files created for both TEST-001 and TEST-002 covering domain event emission
- Test assertion syntax: ReferralReceived event emitted exactly once, payload contains ReferralId, DocumentId, FileType, UploadTimestamp, UploadSource
- All payload fields must be non-null
- FileType must match submitted file type (PDF or TEXT)
- UploadSource must equal "/referrals/intake"
- Tests MUST fail before TASK-014 implementation
