# Task TASK-010 for Referral Intake

## SpecForge Chain Position
This is artefact 7 of 7.

SpecForge Chain:
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → **Task**

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[IMPL]`

This task writes implementation code to make TASK-009's test pass. No test modifications allowed.

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

Test: TEST-002 → Requirement: REQ-001 → Result: HTTP intake must accept valid plain-text document, assign unique ReferralId (UUID), return HTTP 201 with JSON body containing referralId and timestamp (RFC3339) → Approach: Implement HTTP intake handler that handles text file uploads, generates UUID for ReferralId, creates Referral aggregate with ReferralId and ReferralDocument (FileType=TEXT, ContentData from request with UTF-8 encoding preserved), records current timestamp in RFC3339 format, returns HTTP 201 response with JSON body {"referralId": "{UUID}", "timestamp": "{RFC3339 datetime}"}

---

## Commit Instruction
Commit message format (REQUIRED):
```
impl(referral-intake): Implement plain-text acceptance and HTTP response
```

---

## Success Criteria
- Implementation code completes without exceeding test assertions
- TEST-002 PASSES with HTTP 201 response and valid ReferralId (UUID), timestamp (RFC3339)
- Referral aggregate created with assigned ReferralId
- ReferralDocument.FileType set to "TEXT"
- ReferralDocument text content preserves UTF-8 encoding
- ReferralId is unique UUID (no duplicates across multiple requests)
- Timestamp is valid RFC3339 formatted datetime
- Code references ONLY domain concepts: Referral aggregate, ReferralDocument value object
