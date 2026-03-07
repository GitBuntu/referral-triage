# Task TASK-003 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[RED]`

This task writes a new failing test for validating and rejecting oversized documents.

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
- SCENARIO-004

**Test cases this task MUST satisfy**:
- TEST-004 — from ../tests/referral-intake/TEST-004.md

---

## Task Description

Test: TEST-004 → Requirement: REQ-001 → Result: HTTP intake must reject documents exceeding 5 MB with HTTP 413 response containing error message "Request entity too large" and details "Maximum file size: 5 MB", no ReferralId assigned, no Blob Storage write, no ReferralReceived event emitted → Approach: Write test that submits 6 MB PDF document to HTTP intake endpoint, asserts HTTP 413 response status, asserts error message content, asserts no artifacts created in system

---

## Commit Instruction
Commit message format (REQUIRED):
```
test(referral-intake): [RED] Reject oversized documents in intake validation
```

After this commit, the test becomes a static contract. No modifications allowed.

---

## Success Criteria
- Test file created at /tests/referral-intake/TEST-004.md (or equivalent code test)
- Test assertion syntax: HTTP status 413, error message "Request entity too large", no ReferralId in response, no Blob Storage write, no ReferralReceived event
- Test MUST fail before TASK-004 implementation
