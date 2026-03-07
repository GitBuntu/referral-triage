# Task TASK-009 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[RED]`

This task writes a new failing test for accepting valid plain-text documents and assigning ReferralId.

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
- SCENARIO-002

**Test cases this task MUST satisfy**:
- TEST-002 — from ../tests/referral-intake/TEST-002.md

---

## Task Description

Test: TEST-002 → Requirement: REQ-001 → Result: HTTP intake must accept valid plain-text document (500 KB), return HTTP 201 with JSON response containing valid UUID in "referralId" field and RFC3339 timestamp in "timestamp" field → Approach: Write test that submits valid 500 KB plain-text file to HTTP intake endpoint, asserts HTTP 201 response status, asserts response body contains referralId (UUID format) and timestamp (RFC3339 format), asserts Referral aggregate is created with matching ReferralId and FileType=TEXT

---

## Commit Instruction
Commit message format (REQUIRED):
```
test(referral-intake): [RED] Accept valid TEXT and return ReferralId
```

After this commit, the test becomes a static contract. No modifications allowed.

---

## Success Criteria
- Test file created at /tests/referral-intake/TEST-002.md (or equivalent code test)
- Test assertion syntax: HTTP status 201, response contains referralId (UUID), response contains timestamp (RFC3339), Referral aggregate created with FileType=TEXT
- Test MUST fail before TASK-010 implementation
