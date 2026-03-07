# Task TASK-016 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[IMPL]`

This task writes implementation code to make TASK-015's test pass. No test modifications allowed.

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

Test: TEST-001 → Requirement: REQ-001 → Result: Referral aggregate must store ReferralDocument value object with FileType attribute matching submitted file type (PDF) and ContentData size matching submitted file size (2 MB) → Approach: Implement ReferralDocument value object constructor that accepts FileType and ContentData, validates FileType is one of: [PDF, TEXT, SCANNED], validates ContentData is not null and size > 0 and size <= 5MB, stores both attributes as immutable, exposes FileType and ContentData as read-only properties

Test: TEST-002 → Requirement: REQ-001 → Result: Referral aggregate must store ReferralDocument value object with FileType attribute matching submitted file type (TEXT) and ContentData size matching submitted file size (500 KB) → Approach: Implement ReferralDocument value object to handle text content with size preservation

---

## Commit Instruction
Commit message format (REQUIRED):
```
impl(referral-intake): Implement ReferralDocument value object validation
```

---

## Success Criteria
- Implementation code completes without exceeding test assertions
- TEST-001 PASSES: ReferralDocument created with FileType=PDF and correct ContentData size
- TEST-002 PASSES: ReferralDocument created with FileType=TEXT and correct ContentData size
- ReferralDocument is immutable (no setters after construction)
- FileType property is accessible and returns correct value
- ContentData property is accessible and returns content with correct size
- FileType validation: Must be one of [PDF, TEXT, SCANNED]
- ContentData validation: Not null, size > 0, size <= 5,242,880 bytes
- Code references ONLY domain concepts: ReferralDocument value object, validation rules from Context
