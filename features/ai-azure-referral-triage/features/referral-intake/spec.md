# Feature Specification: Referral Intake

---

## 🔗 SpecForge Chain Position
**This is artefact 3 of 7.**

## 🔗 SpecForge Chain
Context → Requirements (1..N) → **Feature** → Scenario → Test → Plan → Tasks  
                                             ↑ You are here

---

## Feature Name
**Referral Intake**

## Feature Purpose
Enable automated ingestion of referral documents via HTTP, validate document properties, store raw documents for audit, and trigger AI triage processing. This feature is the entry point for the referral pipeline.

## Requirements Covered
This feature implements the following requirements:
- REQ-001: Referral Intake Validation and Storage

## Related Requirements (Not Implemented in This Feature)
- REQ-002: AI Triage Classification and Record Creation (implemented in separate feature)
- REQ-003: Daily Metrics Aggregation (implemented in separate feature)

---

## Scenarios

### Scenario 1: Accept Valid Referral Document (PDF)
**File**: `SCENARIO-001.feature` (Gherkin)

**Description**: HTTP POST to intake endpoint with a valid PDF referral document.

**Expected Outcome**: Document is validated, stored in Blob Storage, unique ReferralId is assigned, ReferralReceived event is emitted, and ReferralId is returned to caller.

---

### Scenario 2: Accept Valid Referral Document (Text)
**File**: `SCENARIO-002.feature` (Gherkin)

**Description**: HTTP POST to intake endpoint with a valid plain-text referral document.

**Expected Outcome**: Document is validated, stored in Blob Storage, unique ReferralId is assigned, ReferralReceived event is emitted, and ReferralId is returned to caller.

---

### Scenario 3: Reject Invalid File Type
**File**: `SCENARIO-003.feature` (Gherkin)

**Description**: HTTP POST to intake endpoint with a document in unsupported file type (e.g., Word, Excel, image other than scanned).

**Expected Outcome**: Document is rejected, HTTP 400 error is returned with error message, no ReferralId is assigned, no Blob Storage dump occurs, no event is emitted.

---

### Scenario 4: Reject Oversized Document
**File**: `SCENARIO-004.feature` (Gherkin)

**Description**: HTTP POST to intake endpoint with a document exceeding 5MB size limit.

**Expected Outcome**: Document is rejected, HTTP 413 error is returned with error message, no ReferralId is assigned, no Blob Storage dump occurs, no event is emitted.

---

### Scenario 5: Reject Empty Document
**File**: `SCENARIO-005.feature` (Gherkin)

**Description**: HTTP POST to intake endpoint with a document containing zero bytes or empty content.

**Expected Outcome**: Document is rejected, HTTP 400 error is returned with error message, no ReferralId is assigned, no Blob Storage dump occurs, no event is emitted.

---

## Implementation Notes

### HTTP Endpoint Contract
- **Path**: POST /referrals/intake
- **Request Body**: Multipart form-data with single file part
- **Success Response**: HTTP 201 with JSON body: `{"referralId": "UUID", "timestamp": "RFC3339 datetime"}`
- **Error Response**: HTTP 400/413 with JSON body: `{"error": "string", "details": "string"}`

### File Type Validation
- Accepted types: PDF, plain text (.txt), scanned images (JPEG, PNG)
- Rejected types: Word, Excel, Powerpoint, Archives (ZIP, RAR), Executables, etc.

### Size Validation
- Maximum: 5MB (5242880 bytes)
- Minimum: 1 byte (no empty files)

### Storage Path Convention
- Blob path: `/referrals/incoming/{referralId}` where {referralId} is the UUID assigned at intake

### Event Emission
- Event name: ReferralReceived
- Emitted immediately after document validation and storage
- Consumed by: Blob Trigger for AI Triage Processor (upstream system triggers Blob upload)

---

## SpecForge Contract Rules
- All scenarios MUST be implemented as Gherkin feature files
- Each scenario corresponds to ONE test case (or multiple test cases if complex)
- Scenarios define ONLY observable behavior (inputs, outputs, side effects)
- Scenarios do NOT define implementation details
- All referenced requirements MUST exist in /requirements/ directory

---

## Next Step Directive
Create Gherkin feature files (SCENARIO-001.feature through SCENARIO-005.feature) in the `referral-intake/` folder.  
Then create corresponding test files (TEST-001.md through TEST-005.md) in the `/tests/referral-intake/` folder.
