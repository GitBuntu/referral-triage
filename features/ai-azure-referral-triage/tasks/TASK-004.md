# Task TASK-004 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[IMPL]`

This task writes implementation code to make TASK-003's test pass. No test modifications allowed.

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

Test: TEST-004 → Requirement: REQ-001 → Result: HTTP intake must reject documents exceeding 5 MB with HTTP 413 response containing error message "Request entity too large" and details listing maximum size → Approach: Implement file size validation in HTTP intake handler that measures ContentData.length, rejects sizes > 5242880 bytes with HTTP 413 response body containing error message, returns before any ReferralId assignment or Blob Storage operations

---

## Commit Instruction
Commit message format (REQUIRED):
```
impl(referral-intake): File size validation rejects oversized documents
```

---

## Success Criteria
- Implementation code completes without exceeding test assertions
- TEST-004 PASSES with HTTP 413 response and correct error message
- No ReferralId assigned on oversized document
- No Blob Storage write on oversized document
- No ReferralReceived event emitted on oversized document
- Size limit enforcement: > 5,242,880 bytes = rejected
- Code references ONLY domain concepts: Referral aggregate, ReferralDocument value object size validation
