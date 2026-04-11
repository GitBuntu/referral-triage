# Referral Intake and Triage Pipeline

Medical referral intake, AI-powered triage processing, and metrics aggregation using Azure Functions, SQL Server, and Blob Storage.

## Overview

This project implements a serverless pipeline for processing medical referral documents:

1. **ReferralIntake** (HTTP Trigger) - Accepts PDF/text/image referral documents, validates, stores in Blob Storage, records in SQL
2. **TriageProcessor** (EventGrid Trigger) - Triggered via blob creation event, extracts text (OCR), classifies via GPT-4o, stores TriageRecord in SQL, emits failures to DLQ
3. **MetricsAggregator** (Timer Trigger) - Daily aggregation of triage metrics by specialty, urgency, and processing latency

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
         │ EventGrid on blob creation
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
       │   ├── ValidationService.cs
       │   ├── DeadLetterService.cs
       │   ├── RetryHelper.cs
       │   └── TriageClassificationException.cs
│       ├── Infrastructure/
       │   ├── AzureServiceSettings.cs
       │   ├── ReferralTriageContext.cs (Entity Framework Core)
       │   ├── Referral.cs
       │   ├── TriageRecord.cs
       │   ├── DomainEventLog.cs
       │   ├── User.cs
       │   └── SchemaVersion.cs
│       ├── Program.cs
│       ├── ReferralTriageApp.csproj
│       └── local.settings.json
├── requirements.md
└── README.md (this file)

```

## Azure Resources

| Name | Type |
|------|------|
| Application Insights Smart Detection | Action group |
| ASP-rgreferraltriageprodcanadacentr-8a58 | App Service plan |
| chris-mmq5p9qj-canadaeast | Foundry |
| Failure Anomalies - referral-triage-insights | Smart detector alert rule |
| Failure Anomalies - rtfuncapp | Smart detector alert rule |
| referral-triage-docint | Document Intelligence |
| referral-triage-insights | Application Insights |
| ReferralTriage (sqlsrv-referraltriage-prod-canadacentral-001) | SQL database |
| rtfuncapp | Function App |
| rtfuncapp | Application Insights |
| rtkeyvault | Key vault |
| rtstoredev | Storage account |
| rtstoredev-blob-events | Event Grid System Topic |
| sqlsrv-referraltriage-prod-canadacentral-001 | SQL server |

## Quick Start

### 1. Local Development Setup

Install dependencies and build the project:

```bash
cd src/ReferralTriageApp
dotnet restore
dotnet build
```

### 2. Configure Local Settings

Update `src/ReferralTriageApp/local.settings.json` with Azure service credentials:

```json
{
  "ReferralTriageApp": {
    "DocumentIntelligenceEndpoint": "https://YOUR_REGION.cognitiveservices.azure.com/",
    "DocumentIntelligenceKey": "YOUR_KEY",
    "AzureOpenAiEndpoint": "https://YOUR_RESOURCE.openai.azure.com/",
    "AzureOpenAiKey": "YOUR_KEY",
    "AzureOpenAiDeploymentName": "gpt-4o"
  },
  "ConnectionStrings": {
    "SqlServer": "Server=YOUR_SERVER;Database=ReferralTriage;...",
    "BlobStorage": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;..."
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

Or using Bicep directly (from the `cerebricep` repo root):

```bash
export ENVIRONMENT=dev
export SQL_ADMIN_PASSWORD="$(openssl rand -base64 32 | tr -d '/' | cut -c1-20)"

az deployment sub create \
  --location eastus2 \
  --template-file infra/workloads/referral-triage/main.bicep \
  --parameters infra/workloads/referral-triage/environments/${ENVIRONMENT}.bicepparam \
  --parameters sqlAdminPassword="${SQL_ADMIN_PASSWORD}"
```

See `infra/workloads/referral-triage/DEPLOYMENT-NOTES.md` for the full deployment checklist and what-if / dry-run steps.

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
| `ReferralTriageApp:BlobStorageAccount` | Blob storage account name | `rtstoredev` |
| `ReferralTriageApp:BlobContainer` | Blob container name | `referrals` |
| `ReferralTriageApp:BlobIncomingPath` | Path prefix for incoming documents | `incoming` |
| `ReferralTriageApp:SqlServerDatabase` | SQL Server database name | `ReferralTriage` |
| `ReferralTriageApp:TriageRecordsTableName` | SQL Server table name | `TriageRecord` |
| `ReferralTriageApp:MaxFileSizeBytes` | Max upload file size in bytes | `52428800` (50 MB) |
| `ReferralTriageApp:AllowedFileTypes` | Comma-separated file extensions | `pdf,txt,png,jpg,jpeg` |
| `ReferralTriageApp:AllowedSpecialties` | Comma-separated allowed specialties | `cardiology,orthopaedics,neurology,dermatology,general_medicine` |
| `ReferralTriageApp:AllowedUrgencies` | Comma-separated urgency levels | `routine,soon,urgent` |
| `ReferralTriageApp:MetricsAggregationSchedule` | CRON pattern for daily timer | `0 0 2 * * *` (UTC 02:00 daily) |
| `ReferralTriageApp:ConfidenceThreshold` | Minimum AI confidence score (0.0-1.0) | `0.90` |
| `ReferralTriageApp:DLQName` | Dead-letter queue name | `referral-dlq` |
| `ReferralTriageApp:DocumentIntelligenceEndpoint` | Document Intelligence service endpoint | Required |
| `ReferralTriageApp:DocumentIntelligenceKey` | Document Intelligence service key | Required |
| `ReferralTriageApp:AzureOpenAiEndpoint` | Azure OpenAI service endpoint | Required |
| `ReferralTriageApp:AzureOpenAiKey` | Azure OpenAI service key | Required |
| `ReferralTriageApp:AzureOpenAiDeploymentName` | GPT model deployment name | `gpt-4o` |

### Dead-Letter Queue

Failed referrals are emitted to an Azure Storage Queue specified by `ReferralTriageApp:DLQName`. Each DLQ message contains:

```json
{
  "referralId": "550e8400-e29b-41d4-a716-446655440000",
  "failureReason": "extraction_failed|classification_failed|validation_failed|blob_not_found|unsupported_document_format|invalid_input",
  "errorMessage": "Detailed error message",
  "timestamp": "2024-03-08T10:30:00Z",
  "retryCount": 2
}
```

## Domain Models

### Referral
- `ReferralId` (GUID) - Unique referral identifier
- `DocumentFormat` (string) - File type (pdf, txt, png, jpg, jpeg)
- `DocumentStoragePath` (string) - Blob Storage path
- `DocumentHash` (string) - SHA256 hash
- `Status` (string) - pending, triaging, completed, failed
- `SubmittedAt` (DateTime) - Submission timestamp
- `SubmittedBy` (string) - User identifier
- `CreatedAt`, `ModifiedAt` (DateTime) - Audit timestamps

### TriageRecord
- `TriageRecordId` (GUID) - Unique record identifier
- `ReferralId` (GUID) - Foreign key to Referral
- `Specialty` (string) - cardiology, orthopaedics, neurology, dermatology, general_medicine
- `Urgency` (string) - routine, soon, urgent
- `ExtractedFields` (JSON string) - Dictionary with patient_name, dob, symptoms, duration, red_flags
- `ClinicalSummary` (string) - Max 500 characters
- `ConfidenceScore` (decimal) - 0.0-1.0 AI confidence
- `TriagedAt` (DateTime) - Triage completion time
- `CreatedAt`, `ModifiedAt` (DateTime) - Audit timestamps

### DomainEventLog
- `DomainEventId` (GUID) - Event identifier
- `ReferralId` (GUID) - Associated referral
- `EventType` (string) - Event category
- `CreatedAt` (DateTime) - Event timestamp

### DailyMetrics
- `Id` (string) - Metric date identifier (date-YYYY-MM-DD)
- `MetricDate` (DateTime) - Date of metrics
- `TotalReferralsProcessed` (int) - Count processed
- `ReferralsBySpecialty` (JSON dict) - Specialty breakdown
- `RoutineCount`, `SoonCount`, `UrgentCount` (int) - Urgency breakdowns
- `AverageProcessingLatencyMs` (double) - Mean processing time
- `MissingFieldRates` (JSON dict) - Missing field percentages

## Validation Rules

### ReferralIntakeRequest
- `DocumentData`: Valid base64-encoded file
- `DocumentFormat`: One of (pdf, txt, png, jpg, jpeg)
- `DocumentSize`: ≤ 50 MB (check `MaxFileSizeBytes`)
- `PatientMRN`: Optional

### TriageRecord
- `Specialty`: One of (cardiology, orthopaedics, neurology, dermatology, general_medicine)
- `Urgency`: One of (routine, soon, urgent)
- `ExtractedFields`: All required fields (patient_name, dob, symptoms, duration, red_flags) with non-empty values
- `ClinicalSummary`: 1-500 characters
- `ConfidenceScore`: 0.0-1.0; values < 0.90 recommended for manual review

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

Run unit tests:

```bash
cd src/ReferralTriageApp.Tests
dotnet test
```

Current test coverage:
- RetryHelperTests - Exponential backoff and retry logic
- ValidationServiceTests - Referral and triage record validation
- QualityGatesTests - Confidence score and required fields gates

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

For issues or questions, see:
- `openspec/changes/implement-triage-pipeline/` - Change specification and design decisions
- Application Insights logs - Function execution and errors
- Azure Monitor - Service health and scaling metrics
