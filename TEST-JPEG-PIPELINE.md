# JPEG Document Extraction Pipeline Test

## Overview
Tests the referral triage system's ability to extract and classify JPEG documents using real Azure Blob Storage (not Azurite emulator).

### Prerequisites
- Function app running: `func start` must be active
- SQL Server accessible at `localhost`
- JPEG test file: `triage-test.jpeg` in the working directory

---

## Test Execution

### Option 1: Single Command (Fastest)
```bash
cd /d/source/referral-triage && base64_data=$(base64 -w0 triage-test.jpeg) && printf '{"documentData":"%s","documentFormat":"jpeg"}' "$base64_data" > request.json && curl -s -X POST http://localhost:7071/api/referrals/intake -H "Content-Type: application/json" -d @request.json | head -1
```

### Option 2: Step by Step
```bash
# Step 1: Navigate to project root
cd /d/source/referral-triage

# Step 2: Generate JSON payload with base64-encoded JPEG
python3 encode_request.py

# Step 3: Submit to function
curl -s -X POST http://localhost:7071/api/referrals/intake \
  -H "Content-Type: application/json" \
  -d @request.json
```

---

## Expected Response
```json
{
  "ReferralId": "71d0bb53-ffaf-44c4-9299-faecee52b7c6",
  "BlobUri": "https://rtstoredev.blob.core.windows.net/referrals/incoming/71d0bb53-ffaf-44c4-9299-faecee52b7c6/71d0bb53-ffaf-44c4-9299-faecee52b7c6.jpg",
  "SubmittedAt": "2026-03-28T14:24:07.5536231Z",
  "DocumentFormat": "jpeg",
  "DocumentHash": "19d3a1d0b8caff3010f7c2ffe6d5772845ac868ef5cc65e78f73da1e899000e6",
  "Message": "Referral successfully submitted and queued for triage processing"
}
```

---

## Verify Results

### Query Local SQL Server
```bash
sqlcmd -S localhost -d ReferralTriage -Q "SELECT TOP 1 ReferralId, Specialty, Urgency, ConfidenceScore, TriagedAt FROM TriageRecord ORDER BY TriagedAt DESC" -C
```

### Expected Output
```
ReferralId                           Specialty       Urgency  ConfidenceScore  TriagedAt
71D0BB53-FFAF-44C4-9299-FAECEE52B7C6 general_medicine soon     0.90             2026-03-28 14:24:21.7016310
```

---

## Success Criteria

✅ **JPEG Classification Success** means:
1. **Confidence Score ≥ 0.90** (meets threshold)
2. **Specialty classified** (e.g., general_medicine, cardiology, etc.)
3. **Urgency assigned** (routine, soon, or urgent)
4. **No errors** from Document Intelligence or gpt-4o

---

## Technical Details

### Why This Works
- **File-based payload**: Avoids Windows command-line argument length limits
- **Base64 encoding**: Sends binary JPEG data as JSON-safe string
- **Real Azure storage**: Global SAS URIs (`https://rtstoredev.blob.core.windows.net/...`) are accessible from Azure Document Intelligence service
- **Async processing**: Classification happens in the background; query SQL Server for results

### What the Pipeline Does
1. **ReferralIntakeFunction** receives JSON with base64 `documentData`
2. **DocumentExtractionService** uploads JPEG to Azure Blob Storage
3. Generates global SAS URI (unlike Azurite's localhost-only URIs)
4. **Document Intelligence** extracts text from JPEG using SAS URI
5. **TriageClassificationService** calls gpt-4o with extracted text
6. Enforces confidence threshold (≥ 0.90)
7. **TriageProcessingService** stores result in local SQL Server's `TriageRecord` table

---

## Troubleshooting

### Error: "Argument list too long"
- Use file-based approach (Option 2), not command-line expansion
- See: `printf '...' > request.json` method

### Error: "must be valid base64-encoded data"
- Ensure `request.json` doesn't contain literal `$(base64 ...)` string
- Use `encode_request.py` to safely write file

### Confidence Score < 0.90
- Indicates JPEG extraction or classification quality issues
- Check function logs: `func start` terminal
- Verify Document Intelligence can reach blob SAS URI

### TriageRecord not found
- Processing may still be in-flight (async)
- Wait a few seconds and retry query
- Check function logs for Document Intelligence errors
