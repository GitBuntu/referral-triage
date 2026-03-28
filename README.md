# Referral Intake and Triage Pipeline

Medical referral intake, AI-powered triage processing, and metrics aggregation using Azure Functions, SQL Server, and Blob Storage.

## Overview

This project implements a serverless pipeline for processing medical referral documents:

1. **ReferralIntake** (HTTP Trigger) - Accepts PDF/text referral documents, validates file type/size, stores in Blob Storage
2. **TriageProcessor** (Blob Trigger) - Extracts text from documents (OCR), calls AI model for classification, stores triage records in SQL Server
3. **MetricsAggregator** (Timer Trigger) - Aggregates daily metrics (specialty counts, urgency levels, processing latency)

## Architecture

```
┌─────────────────┐
│  HTTP Endpoint  │
│ ReferralIntake  │
└────────┬────────┘
         │ File upload
         ▼
┌─────────────────┐
│  Blob Storage   │
│  /referrals/    │
└────────┬────────┘
         │ Trigger on blob
         ▼
┌─────────────────┐      ┌─────────────────┐
│TriageProcessor  │──────│Document Intel.  │ OCR
└────────┬────────┘      └─────────────────┘
         │
         │ AI Model
         ▼
┌─────────────────┐      ┌─────────────────┐
│  Azure OpenAI   │──────│TriageProcessor  │
└─────────────────┘      └────────┬────────┘
                                  │
                                  ▼
                        ┌─────────────────┐
                        │  SQL Server     │
                        │TriageRecords &  │
                        │   Metrics       │
                        └─────────────────┘
                                  ▲
                                  │ Query daily
                        ┌─────────┴────────┐
                        │ MetricsAggregator│
                        │   (Timer daily)  │
                        └──────────────────┘
```

## Prerequisites

- .NET 8 SDK or later
- Azure CLI with Azure Developer CLI (`azd`) installed
- Azure subscription with permissions to create resources
- Document Intelligence service (for OCR)
- Azure OpenAI service (for AI classification)
- SQL Server database (for storing triage records and metrics)

## Project Structure

```
referral-triage/
├── src/
│   └── ReferralTriageApp/
│       ├── Functions/
│       │   ├── ReferralIntakeFunction.cs
│       │   ├── TriageProcessorFunction.cs
│       │   └── MetricsAggregatorFunction.cs
│       ├── Models/
│       │   └── DomainModels.cs
│       ├── Services/
│       │   ├── IServices.cs
│       │   ├── ReferralIntakeService.cs
│       │   ├── TriageProcessingService.cs
│       │   ├── DocumentExtractionService.cs
│       │   ├── TriageClassificationService.cs
│       │   ├── MetricsAggregationService.cs
│       │   └── ValidationService.cs
│       ├── Infrastructure/
│       │   ├── ReferralTriageSettings.cs
│       │   └── DbContext.cs (Entity Framework Core)
│       ├── Program.cs
│       ├── ReferralTriageApp.csproj
│       └── local.settings.json
├── infra/
│   ├── main.bicep
│   ├── storage.bicep
│   ├── sqlserver.bicep
│   ├── functionapp.bicep
│   ├── aiservices.bicep
│   └── keyvault.bicep
├── azure.yaml
├── requirements.md
└── README.md (this file)
```

## Quick Start

### 1. Local Development Setup

Install dependencies and build the project:

```bash
cd src/ReferralTriageApp
dotnet restore
dotnet build
```

### 2. Configure Local Settings

Update `src/ReferralTriageApp/local.settings.json` with connection strings for local development:

```json
{
  "ReferralTriageSettings": {
    "DocumentIntelligenceEndpoint": "https://YOUR_REGION.api.cognitive.microsoft.com/",
    "DocumentIntelligenceKey": "YOUR_KEY",
    "AzureOpenAiEndpoint": "https://YOUR_RESOURCE.openai.azure.com/",
    "AzureOpenAiKey": "YOUR_KEY",
    "AzureOpenAiDeploymentName": "gpt-4"
  }
}
```

### 3. Run Locally

```bash
cd src/ReferralTriageApp
func start
```

### 4. Deploy to Azure

Using Azure Developer CLI:

```bash
azd up
```

Or using Bicep directly:

```bash
az deployment group create \
  --resource-group referral-triage-rg \
  --template-file infra/main.bicep \
  --parameters location=eastus environment=dev projectName=referral-triage
```

## API Usage

### Submit Referral (HTTP POST)

```bash
curl -X POST http://localhost:7071/api/referrals/intake \
  -H "Content-Type: application/json" \
  -d '{
    "documentData": "base64-encoded-file-contents",
    "documentFormat": "pdf",
    "patientMRN": "12345"
  }'
```

**Response:**
```json
{
  "referralId": "550e8400-e29b-41d4-a716-446655440000",
  "blobUri": "https://...",
  "submittedAt": "2024-03-08T10:30:00Z",
  "documentFormat": "pdf",
  "documentHash": "abc123...",
  "message": "Referral successfully submitted and queued for triage processing"
}
```

### Supported File Types

- PDF (`.pdf`)
- Plain Text (`.txt`)
- PNG Image (`.png`)
- JPEG Image (`.jpg`, `.jpeg`)

## Configuration

### Environment Variables

Configure via `local.settings.json` or Azure Function App Settings:

| Setting | Description | Default |
|---------|-------------|---------|
| `ReferralTriageApp:BlobStorageAccount` | Blob storage account name | `referralstoragedev` |
| `ReferralTriageApp:BlobContainer` | Blob container name | `referrals` |
| `ReferralTriageApp:BlobIncomingPath` | Blob path where incoming docs are stored | `incoming` |
| `ReferralTriageApp:SqlServerDatabase` | SQL Server database name | `referral_triage` |
| `ReferralTriageApp:TriageRecordsTableName` | SQL Server table name | `triage_records` |
| `ReferralTriageApp:MaxFileSizeBytes` | Max upload file size | `52428800` (50 MB) |
| `ReferralTriageApp:AllowedFileTypes` | Comma-separated file types | `pdf,txt,png,jpg,jpeg` |
| `ReferralTriageApp:AllowedSpecialties` | Comma-separated specialties | `cardiology,orthopaedics,neurology,dermatology,general_medicine` |
| `ReferralTriageApp:AllowedUrgencies` | Comma-separated urgency levels | `routine,soon,urgent` |
| `ReferralTriageApp:MetricsAggregationSchedule` | CRON expression for timer | `0 0 2 * * *` (daily 2 AM) |
| `ReferralTriageApp:ConfidenceThreshold` | AI confidence score threshold for auto-completion (0.0-1.0) | `0.90` |
| `ReferralTriageApp:DLQName` | Azure Storage Queue name for dead-lettered referrals | `referral-dlq` |

### Quality Gates Configuration

The triage pipeline applies quality gates to determine whether a referral can be auto-completed or requires manual review:

1. **Confidence Score Gate**: AI confidence score must be >= `ReferralTriageApp:ConfidenceThreshold`
2. **Required Fields Gate**: All required extracted fields must be populated (non-empty):
   - `patient_name`
   - `dob` (date of birth)
   - `symptoms`
   - `duration`
   - `red_flags`

If **both** gates pass, referral status is set to `"completed"` (auto-completion).
If **either** gate fails, referral status is set to `"pending_review"` (manual review required).

### Dead-Letter Queue

Failed referrals are emitted to an Azure Storage Queue specified by `ReferralTriageApp:DLQName`. Each DLQ message contains:

```json
{
  "referralId": "550e8400-e29b-41d4-a716-446655440000",
  "failureReason": "document_extraction_failed|classification_failed|validation_failed|...",
  "errorMessage": "Detailed error message",
  "timestamp": "2024-03-08T10:30:00Z",
  "retryCount": 2
}
```

Failure reasons:
- `document_extraction_failed` - Text extraction from document failed after max retries
- `classification_failed` - AI classification failed after max retries
- `validation_failed` - Triage record validation failed
- `blob_not_found` - Document blob not found in Blob Storage
- `blob_access_denied` - Access denied when reading document blob
- `unsupported_document_format` - File extension not supported
- `invalid_input` - Referral ID or file name missing/invalid
- `operation_timeout` - Processing timed out
- `unexpected_error` - Unexpected application error

## How GPT-4o Is Inferring Your Data (Based on Your Code)

Your system uses multiple mechanisms to ensure reliable data inference from medical referral documents. Understanding these mechanisms reveals why GPT-4o can accurately extract structured data rather than simply "guessing."

### 1. Explicit Instructions via Prompts

GPT-4o receives two levels of instruction:

**System Prompt** (the core instruction):
- Defines the role: "You are a medical triage specialist"
- Sets constraints: "Extract only valid specialties and urgency levels"
- Specifies output format: "Return a JSON object with these exact fields"

**User Prompt** (the task):
- The actual referral document text (extracted via Document Intelligence)
- Explicit field requirements: "Extract patient_name, dob, symptoms, duration, red_flags"
- Examples of valid values: "Specialties: cardiology, orthopaedics, neurology, dermatology, general_medicine"

GPT-4o reads these instructions and knows exactly what to extract. It's not inferring your intent—you're explicitly telling it.

### 2. Structured Function Calling (Not Just "Asking")

This is the key mechanism that prevents hallucination. Rather than asking GPT-4o to "return JSON" (which it might format incorrectly), you use **function calling** with a strict schema:

```json
{
  "name": "classify_referral",
  "parameters": {
    "type": "object",
    "properties": {
      "specialty": {
        "type": "string",
        "enum": ["cardiology", "orthopaedics", "neurology", "dermatology", "general_medicine"]
      },
      "urgency": {
        "type": "string",
        "enum": ["routine", "soon", "urgent"]
      },
      "patient_name": { "type": "string" },
      "dob": { "type": "string", "format": "date" },
      "symptoms": { "type": "string" },
      "duration": { "type": "string" },
      "red_flags": { "type": "string" },
      "confidence_score": { "type": "number", "minimum": 0, "maximum": 1 }
    },
    "required": ["specialty", "urgency", "confidence_score", "patient_name", "dob", "symptoms", "duration", "red_flags"]
  }
}
```

**What this does:** GPT-4o must return exactly this structure. It cannot hallucinate. It can only:
- Pick `specialty` from the enum (cardiology, orthopaedics, neurology, dermatology, or general_medicine)
- Pick `urgency` from the enum (routine, soon, or urgent)
- Fill in required string/date fields with extracted values
- Return a confidence_score between 0 and 1

This is **constraint-based inference**—the schema enforces data integrity at the source.

### 3. Quality Gates After Classification

After GPT-4o returns its classification, your system applies validation gates:

**Confidence Score Gate:**
```csharp
if (triageResult.ConfidenceScore < 0.90) {
    status = "pending_review";  // Reject low-confidence results
}
```

GPT-4o returns a `confidence_score` (0-1). You reject anything < 0.90. This is where you're saying: *"I don't trust this inference—escalate to human review."*

**Required Fields Gate:**
```csharp
var requiredFields = new[] { "patient_name", "dob", "symptoms", "duration", "red_flags" };
if (requiredFields.Any(f => string.IsNullOrEmpty(extractedFields[f]))) {
    status = "pending_review";  // Reject incomplete extractions
}
```

If either gate fails, the referral is flagged for manual review rather than auto-completion.

### 4. Fallback: Keyword Heuristics

If Azure OpenAI isn't available or fails, your system has a mock classification fallback:

```csharp
// Fallback pattern matching when AI is unavailable
if (text.Contains("chest pain", StringComparison.OrdinalIgnoreCase)) {
    specialty = "cardiology";
    urgency = "urgent";
}
else if (text.Contains("fracture") || text.Contains("orthopedic")) {
    specialty = "orthopaedics";
    urgency = "soon";
}
// ... more rules
```

This shows: *Even your fallback is pattern matching, not "understanding."* It's deterministic and predictable.

### The Real Answer: Why This Works

GPT-4o infers your desired data through a combination of mechanisms:

1. **Explicit Prompt** – Your system + user prompts tell it exactly what role to play and what to extract
2. **Forced Structure** – Function calling + enums constrain the output shape; it can't deviate
3. **Confidence Scoring** – You provide the threshold (0.90); GPT-4o exposes its uncertainty
4. **Validation Loop** – You catch when specialty/urgency don't match allowed values and escalate to manual review
5. **Fallback Heuristics** – Pattern matching provides deterministic behavior when AI is unavailable

### What GPT-4o Cannot Do Without You Telling It

- **It has no idea your confidence threshold is 0.90** without you logging it in code and enforcing it at runtime
- **It doesn't know which fields are "required"** unless you include them in the JSON schema's `required` array
- **It can't invent specialties** – they're locked to your enum list (cardiology, orthopaedics, etc.)
- **It doesn't understand your downstream validation** – if a specialty/urgency value doesn't match your database constraints, that's caught by your validation rules, not GPT-4o

### The Uncertainty: When Confidence < 0.90

When GPT-4o returns `confidence_score < 0.90`, that's it saying: *"I saw patterns in the document, but I wasn't sure."*

Your system correctly treats this as unreliable and escalates to manual review. This is the appropriate response—GPT-4o is exposing its uncertainty rather than confidently returning a wrong answer.

**In summary:** You're not relying on GPT-4o to "understand" medicine. You're using it as a pattern-matching engine with guardrails. The schema, prompts, confidence thresholds, and validation rules are your safety net.

## Domain Models

### ReferralDocument
- `id` (string) - Unique referral ID
- `documentFormat` (string) - File type (pdf, txt, png, jpg, jpeg)
- `blobPath` (string) - Azure Blob Storage path
- `documentHash` (string) - SHA256 hash
- `submittedAt` (DateTime) - Submission timestamp
- `patientMRN` (string, optional) - Patient Medical Record Number
- `status` (string) - pending, processing, completed, failed

### TriageRecord
- `id` (string) - Referral ID
- `specialty` (string) - Classified specialty
- `urgency` (string) - Classification urgency level
- `extractedFields` (Dictionary) - Required: patient_name, dob, symptoms, duration, red_flags
- `clinicalSummary` (string) - Max 500 characters
- `confidenceScore` (double) - AI confidence 0.0-1.0

### DailyMetrics
- `id` (string) - Metric date identifier
- `metricDate` (DateTime) - Date of metrics
- `totalReferralsProcessed` (int) - Count
- `referralsBySpecialty` (Dictionary) - Specialty breakdown
- `routineCount`, `soonCount`, `urgentCount` (int) - Urgency counts
- `averageProcessingLatencyMs` (double) - Processing latency
- `missingFieldRates` (Dictionary) - Missing field percentages

## Validation Rules

### ReferralIntakeRequest
- DocumentData: Valid base64, 1 byte to 50 MB
- DocumentFormat: One of allowed types
- PatientMRN: Optional

### TriageRecord
- Specialty: Must be one of allowed specialties
- Urgency: Must be one of allowed urgencies
- ExtractedFields: All required fields with non-empty values
- ClinicalSummary: 1-500 characters

## Monitoring & Diagnostics

View Application Insights metrics:

```bash
az monitor app-insights metrics show \
  --resource-group referral-triage-rg \
  --app-id <app-insights-id> \
  --metric requests/count
```

View Function logs:

```bash
func azure functionapp logstream <function-app-name>
```

## Testing

```bash
cd src/ReferralTriageApp
dotnet test
```

## Cost Optimization

- **Blob Storage**: Hot tier for active referrals, Archive for >90 days
- **SQL Server**: Elastic pool or provisioned database tier for variable workloads
- **Functions**: Consumption plan with auto-scaling (Y1)
- **Document Intelligence**: Pay-per-use (S0)
- **OpenAI**: Deployment-based with standard capacity

## Security

- Managed identities for Function App → Azure resources
- Network isolation via VNet (optional enhancement)
- Secrets stored in Azure Key Vault
- Minimum TLS 1.2 enforcement
- No direct blob public access

## Scaling Considerations

- **ReferralIntake**: Auto-scales to handle HTTP volume
- **TriageProcessor**: Scales with blob events (max 100 concurrent)
- **MetricsAggregator**: Single daily execution via timer
- **SQL DB**: Serverless auto-scaling (default 400 RU)

## Troubleshooting

### 401 Unauthorized on Azure Services
- Check Function App managed identity has required roles
- Verify service credentials in Key Vault

### 429 Rate Limiting
- Increase SQL Server DTU or scale up elastic pool
- Enable autoscale for elastic pools
- Implement exponential backoff in retries

### Document Extraction Fails
- Verify Document Intelligence credentials
- Check document format support
- Ensure document is not corrupted/encrypted

## References

- [Azure Functions Documentation](https://learn.microsoft.com/azure/azure-functions/)
- [Azure SQL Database](https://learn.microsoft.com/azure/azure-sql/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [Document Intelligence API](https://learn.microsoft.com/azure/ai-services/document-intelligence/)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)

## License

See LICENSE file.

## Support

For issues or questions, consult the project's SpecForge documentation in the `features/` directory.
