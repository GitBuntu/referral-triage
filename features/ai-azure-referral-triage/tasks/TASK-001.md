# Task TASK-001 for referral-intake

---

## 🔗 SpecForge Chain Position
**This is artefact 7 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → Feature → Scenario → Test → Plan → Task

You are defining: Task.

---

## TDD Cycle Phase (CRITICAL)
**Phase**: `[RED]`

---

## Test Immutability Constraint (TDD Discipline)

⚠️ **IF Phase = [RED]**:
- You are WRITING a new test file validating document format validation behavior
- This test MUST fail initially (proving test is real and validating something)
- Commit message MUST include `[RED]`: `test(referral-intake): [RED] validate document format`
- Once committed, this test becomes a **static contract**

---

## SpecForge Contract
This artefact is governed by the SpecForge Contract. Strict enforcement rules:

1. Do NOT reinterpret any requirement. COPY Requirement Statement sections verbatim from `/requirements/REQ-001.md`.
2. Do NOT summarise or simplify requirement text. COPY complete text without omission.
3. Do NOT infer missing behaviour. COPY ONLY what exists in requirements and tests.
4. Do NOT add assumptions. Reference ONLY assertions explicitly in `TEST-001.md` Assertions sections.
5. Do NOT introduce new invariants, domain events, or states. Copy only from Context and requirements.

---

# Requirements (Verbatim Copy-Forward)

- REQ-001: When a clinical operations coordinator submits a referral document via HTTP POST, the Referral aggregate shall validate that the ReferralDocument has a supported format (pdf, text, image), size between 1 byte and 50 MB, and non-empty storage path. If valid, Referral shall assign a unique ReferralId, store the document metadata, and emit Referral-Submitted event with ReferralId, DocumentFormat, DocumentHash, and SubmittedAt payload. The submitted Referral aggregate shall then be immediately retrievable from IReferralRepository by ReferralId.

---

# Scenarios and Tests Covered
This Task implements behaviour validated by tests listed below.

**Scenarios covered**:
- SCENARIO-001

**Test cases this task MUST satisfy**:
- TEST-001 — from `/tests/referral-intake/TEST-001.md`

---

# Task Description

```
Planning Step Reference: Step 1: Referral must Format in {pdf, text, image} → Tests TEST-001 → Validates REQ-001

Test Reference: TEST-001

Requirement: REQ-001

Assertion: Assert: ReferralDocument.Format in {pdf, text, image}

Approach: Write test that validates ReferralDocument constructor rejects formats outside of {pdf, text, image} and accepts formats within the set
```

---

# TDD Verification Checklist (Pre-Completion)

**REQUIRED validations before marking Status = COMPLETE:**

### If Phase = [RED]:
- ✅ Test file created: Test written validating document format validation
- ✅ Test is failing: Test fails initially with no implementation
- ✅ Commit signed: Message includes `test(referral-intake): [RED]` marker
- ✅ No test mutations: Test file is static until [IMPL] phase

---

# Completion State
Status: NOT STARTED
Completed By: 
Completed On:
