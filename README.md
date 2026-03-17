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
| `BlobStorageAccount` | Blob storage account name | `referralstoragedev` |
| `BlobContainer` | Blob container name | `referrals` |
| `SqlServerDatabase` | SQL Server database name | `referral_triage` |
| `TriageRecordsTableName` | SQL Server table name | `triage_records` |
| `MaxFileSizeBytes` | Max upload file size | `52428800` (50 MB) |
| `AllowedFileTypes` | Comma-separated file types | `pdf,txt,png,jpg,jpeg` |
| `AllowedSpecialties` | Comma-separated specialties | `cardiology,orthopaedics,neurology,dermatology,general_medicine` |
| `AllowedUrgencies` | Comma-separated urgency levels | `routine,soon,urgent` |
| `MetricsAggregationSchedule` | CRON expression for timer | `0 0 2 * * *` (daily 2 AM) |

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
