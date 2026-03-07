# Task TASK-001 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[RED]`

This task writes a new failing test for validating and rejecting unsupported file types.

---

## SpecForge Contract
This artefact is governed by the SpecForge Contract.

1. Do NOT interpret any requirement — COPY verbatim only.
2. Do NOT summarise requirement text — COPY complete Requirement Statement sections.
3. Do NOT infer missing behaviour — COPY only what exists in requirements and tests.
4. Do NOT add assumptions beyond test assertions.
5. Do NOT remove constraints — INCLUDE all assertions from test files.
6. Do NOT introduce new invariants, domain events, or states.
7. Do NOT code beyond test assertions — Task complete when ALL test cases pass.
8. Do NOT reference infrastructure, frameworks, or patterns — Use ONLY domain concepts.

---

## Requirements (Verbatim Copy-Forward)
The following requirements MUST be copied forward exactly as written:

- REQ-001: When a referral document is submitted via HTTP POST to the referral intake endpoint, the Referral aggregate shall validate file type (PDF, text, or scanned image), validate content size does not exceed 5MB, accept the valid document, store the raw ReferralDocument in Blob Storage at /referrals/incoming/{referralId}, assign a unique ReferralId, emit ReferralReceived domain event with required payload fields, and return the ReferralId to the HTTP caller.

---

## Scenarios and Tests Covered
**Scenarios covered** (from Plan):
- SCENARIO-003

**Test cases this task MUST satisfy**:
- TEST-003 — from ../tests/referral-intake/TEST-003.md

---

## Task Description

Test: TEST-003 → Requirement: REQ-001 → Result: HTTP intake must reject unsupported file type (Word .docx) with HTTP 400 response containing error message "Unsupported file type" and details "Accepted types: PDF, plain text (.txt), scanned images (JPEG, PNG)", no ReferralId assigned, no Blob Storage write, no ReferralReceived event emitted → Approach: Write test that submits Word document to HTTP intake endpoint, asserts HTTP 400 response status, asserts error message content, asserts no artifacts created in system

---

## Commit Instruction
Commit message format (REQUIRED):
```
test(referral-intake): [RED] Reject unsupported file types in intake validation
```

After this commit, the test becomes a static contract. No modifications allowed.

---

## Success Criteria
- Test file created at /tests/referral-intake/TEST-003.md (or equivalent code test)
- Test assertion syntax: HTTP status 400, error message "Unsupported file type", no ReferralId in response, no Blob Storage write, no ReferralReceived event
- Test MUST fail before TASK-002 implementation
