# Test TEST-004: Oversized Document Rejection

---

## 🔗 SpecForge Chain Position
**This is artefact 5 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → Feature → Scenario → **Test** → Plan → Tasks  
                                                         ↑ You are here

---

## Summary
Verify that a referral document exceeding the 5 MB size limit is rejected at validation, no ReferralId is assigned, no Blob Storage write occurs, and no ReferralReceived event is emitted.

## Primary Requirement
REQ-001: Referral Intake Validation and Storage

## Related Scenarios
SCENARIO-004.feature: Reject Oversized Referral Document

---

## Test Assertions

### Invariant Assertions
Document the invariants that remain true after this test passes:

1. **Invariant: Each Referral must have exactly one TriageRecord after successful triage processing completes.**
   - After rejection: No Referral aggregate is created (validation failed before creation)
   - Status: Not tested (invariant applies only to successfully created referrals)

2. **Invariant: ReferralDocument must be preserved in original form for audit, separate from TriageRecord.**
   - After rejection: No ReferralDocument or TriageRecord is created
   - Status: Not tested (invariant applies only to successfully created referrals)

---

### Domain Event Assertions
Document the domain events that must be emitted when this test passes:

1. **Event: ReferralReceived (Must NOT be emitted)**
   - Assert: No ReferralReceived event is emitted when validation fails
   - Assert: No downstream systems receive intake notification

---

### Outcome Assertions
Document the measurable outcomes that define test success:

1. **HTTP Response Status**
   - Assert: HTTP response status code equals 413 (Payload Too Large)

2. **HTTP Response Body**
   - Assert: Response JSON contains "error" field with value "Request entity too large"
   - Assert: Response JSON contains "details" field with text including "Maximum file size: 5 MB"
   - Assert: Response JSON cannot contain "referralId" field

3. **Blob Storage — No Write**
   - Assert: No file is written to Blob Storage `/referrals/incoming/` directory
   - Assert: No file trace exists in audit logs for this submission

4. **Referral Aggregate — Not Created**
   - Assert: No Referral aggregate is created in database
   - Assert: No ReferralId is assigned
   - Assert: No state change occurs in the system

---

## SpecForge Rules
- This test is PASS/FAIL with no partial credit
- All assertions must be true for the test to pass (especially the negative assertion: ReferralReceived NOT emitted)
- Test is independent of other tests (can run in any order)
- Test setup must create clean state (no side effects from prior test runs)

---

## Next Step Directive
Create corresponding test implementation (code) that executes ALL assertions above.
