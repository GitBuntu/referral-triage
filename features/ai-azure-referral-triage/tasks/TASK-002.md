# Task TASK-002 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[IMPL]`

This task writes implementation code to make TASK-001's test pass. No test modifications allowed.

---

## SpecForge Contract
This artefact is governed by the SpecForge Contract.

1. Do NOT interpret any requirement — COPY verbatim only.
2. Do NOT summarise requirement text — COPY complete Requirement Statement sections.
3. Do NOT infer missing behaviour — COPY only what exists in requirements and tests.
4. Do NOT add assumptions beyond test assertions.
5. Do NOT remove constraints — INCLUDE all assertions from test files.
6. Do NOT introduce new invariants, domain events, or states.
7. Do NOT code beyond test assertions — Task complete when TEST-003 passes.
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

Test: TEST-003 → Requirement: REQ-001 → Result: HTTP intake must reject unsupported file type (Word .docx) with HTTP 400 response containing error message "Unsupported file type" and details listing accepted types → Approach: Implement file type validation in HTTP intake handler that checks ContentData file signature or extension against whitelist of accepted types (PDF, TEXT, SCANNED image formats), rejects non-matching types with HTTP 400 response body containing error message, returns before any ReferralId assignment or Blob Storage operations

---

## Commit Instruction
Commit message format (REQUIRED):
```
impl(referral-intake): File type validation rejects unsupported types
```

---

## Success Criteria
- Implementation code completes without exceeding test assertions
- TEST-003 PASSES with HTTP 400 response and correct error message
- No ReferralId assigned on invalid file type
- No Blob Storage write on invalid file type
- No ReferralReceived event emitted on invalid file type
- Code references ONLY domain concepts: Referral aggregate, ReferralDocument value object, file type validation rule from Context
