# Test TEST-001: Valid PDF Referral Document Acceptance

---

## 🔗 SpecForge Chain Position
**This is artefact 5 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → Feature → Scenario → **Test** → Plan → Tasks  
                                                         ↑ You are here

---

## Summary
Verify that a valid PDF referral document submitted via HTTP POST is accepted, validated, stored in Blob Storage with the assigned ReferralId, and triggers a ReferralReceived domain event.

## Primary Requirement
REQ-001: Referral Intake Validation and Storage

## Related Scenarios
SCENARIO-001.feature: Accept Valid PDF Referral Document

---

## Test Assertions

### Invariant Assertions
Document the invariants that remain true after this test passes:

1. **Invariant: Each Referral must have exactly one TriageRecord after successful triage processing completes.**
   - After acceptance: Referral aggregate is created with ReferralId assigned but TriageRecord is null (awaiting AI processing)
   - Status: Satisfied (invariant will be satisfied later by REQ-002)

2. **Invariant: ReferralDocument must be preserved in original form for audit, separate from TriageRecord.**
   - After acceptance: ReferralDocument with unmodified binary content is persisted in Blob Storage
   - Status: Satisfied (document preserved in original form)

---

### Domain Event Assertions
Document the domain events that must be emitted when this test passes:

1. **Event: ReferralReceived**
   - Triggers on: Successful document validation and storage
   - Payload must contain: ReferralId (UUID), DocumentId (UUID), FileType (PDF), UploadTimestamp (RFC3339 datetime), UploadSource (string = "/referrals/intake")
   - Assert: Event must be emitted exactly once per test execution
   - Assert: All payload fields must be non-null and have correct syntax

---

### Outcome Assertions
Document the measurable outcomes that define test success:

1. **HTTP Response Status**
   - Assert: HTTP response status code equals 201 (Created)

2. **HTTP Response Body**
   - Assert: Response JSON contains "referralId" field with valid UUID format
   - Assert: Response JSON contains "timestamp" field with valid RFC3339 datetime
   - Assert: Response JSON cannot contain null or empty values in either field

3. **Blob Storage Persistence**
   - Assert: File exists in Blob Storage at exact path `/referrals/incoming/{referralId}` where {referralId} matches HTTP response
   - Assert: Stored file binary content is identical to submitted PDF (byte-for-byte comparison)
   - Assert: File metadata (size, upload time) matches submission

4. **Referral Aggregate State**
   - Assert: Referral aggregate exists with ReferralId matching HTTP response
   - Assert: Referral.ReferralDocument.FileType equals "PDF"
   - Assert: Referral.ReferralDocument.ContentData size matches submitted file size
   - Assert: Referral.TriageRecord is null (not yet created)

---

## SpecForge Rules
- This test is PASS/FAIL with no partial credit
- All assertions must be true for the test to pass
- Test is independent of other tests (can run in any order)
- Test setup must create clean state (no side effects from prior test runs)

---

## Next Step Directive
Create corresponding test implementation (code) that executes ALL assertions above.
