# Task TASK-014 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[IMPL]`

This task writes implementation code to make TASK-013's test pass. No test modifications allowed.

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

Test: TEST-001 → Requirement: REQ-001 → Result: Referral aggregate must emit ReferralReceived event with payload ReferralId (UUID), DocumentId (UUID), FileType (FileType=PDF for PDF document), UploadTimestamp (RFC3339 datetime), UploadSource ("/referrals/intake") exactly once → Approach: Implement Referral aggregate to emit ReferralReceived domain event in constructor or create method, populating all required payload fields: ReferralId (from parameter), DocumentId (generated UUID), FileType (from ReferralDocument.FileType), UploadTimestamp (RFC3339 formatted current time), UploadSource (literal string "/referrals/intake"), emit event once before returning HTTP 201

Test: TEST-002 → Requirement: REQ-001 → Result: Referral aggregate must emit ReferralReceived event with payload ReferralId (UUID), DocumentId (UUID), FileType (FileType=TEXT for text document), UploadTimestamp (RFC3339 datetime), UploadSource ("/referrals/intake") exactly once → Approach: Implement Referral aggregate to emit ReferralReceived event with FileType set based on ReferralDocument.FileType value (TEXT for text files)

---

## Commit Instruction
Commit message format (REQUIRED):
```
impl(referral-intake): Implement domain event emission
```

---

## Success Criteria
- Implementation code completes without exceeding test assertions
- TEST-001 PASSES: ReferralReceived event emitted with all payload fields for PDF
- TEST-002 PASSES: ReferralReceived event emitted with all payload fields for TEXT
- Event emitted exactly once per document acceptance (no duplicate events)
- All payload fields present and non-null
- FileType payload field matches ReferralDocument.FileType (PDF or TEXT)
- UploadSource payload field equals "/referrals/intake"
- ReferralId payload field matches assigned ReferralId
- DocumentId payload field is unique UUID
- UploadTimestamp payload field is valid RFC3339 datetime
- Code references ONLY domain concepts: Referral aggregate, ReferralReceived domain event, ReferralDocument
