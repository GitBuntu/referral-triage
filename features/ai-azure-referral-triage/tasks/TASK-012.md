# Task TASK-012 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[IMPL]`

This task writes implementation code to make TASK-011's test pass. No test modifications allowed.

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

Test: TEST-001 → Requirement: REQ-001 → Result: ReferralDocument must be stored in Blob Storage at exact path `/referrals/incoming/{referralId}` with binary content identical to submitted file (byte-for-byte comparison) → Approach: Implement Blob Storage persistence that takes ReferralDocument (with ContentData bytes), constructs path `/referrals/incoming/{referralId}`, uploads bytes to Blob Storage, verifies upload completion before returning HTTP response

Test: TEST-002 → Requirement: REQ-001 → Result: ReferralDocument must be stored in Blob Storage at exact path `/referrals/incoming/{referralId}` with text content identical to submitted file (character-for-character comparison, UTF-8 encoding preserved) → Approach: Implement Blob Storage persistence for text files that preserves UTF-8 encoding, uploads text content to `/referrals/incoming/{referralId}`, verifies encoding preservation

---

## Commit Instruction
Commit message format (REQUIRED):
```
impl(referral-intake): Implement Blob Storage persistence for documents
```

---

## Success Criteria
- Implementation code completes without exceeding test assertions
- TEST-001 PASSES: PDF file stored and retrieved with matching binary content
- TEST-002 PASSES: TEXT file stored with UTF-8 encoding preserved and matching content
- Blob Storage path construction: `/referrals/incoming/{referralId}`
- ReferralId in path matches ReferralId assigned to Referral aggregate
- Files are uploaded before HTTP 201 response is sent
- Code references ONLY domain concepts: ReferralDocument value object, Blob Storage for audit document preservation
