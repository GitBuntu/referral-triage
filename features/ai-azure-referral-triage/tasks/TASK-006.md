# Task TASK-006 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[IMPL]`

This task writes implementation code to make TASK-005's test pass. No test modifications allowed.

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
- SCENARIO-005

**Test cases this task MUST satisfy**:
- TEST-005 — from ../tests/referral-intake/TEST-005.md

---

## Task Description

Test: TEST-005 → Requirement: REQ-001 → Result: HTTP intake must reject empty documents (0 bytes) with HTTP 400 response containing error message "Document is empty" and details listing minimum size → Approach: Implement minimum file size validation in HTTP intake handler that measures ContentData.length, rejects sizes == 0 with HTTP 400 response body containing error message "Document is empty", returns before any ReferralId assignment or Blob Storage operations

---

## Commit Instruction
Commit message format (REQUIRED):
```
impl(referral-intake): File size validation rejects empty documents
```

---

## Success Criteria
- Implementation code completes without exceeding test assertions
- TEST-005 PASSES with HTTP 400 response and correct error message
- No ReferralId assigned on empty document
- No Blob Storage write on empty document
- No ReferralReceived event emitted on empty document
- Size limit enforcement: 0 bytes = rejected
- Code references ONLY domain concepts: Referral aggregate, ReferralDocument value object size validation
