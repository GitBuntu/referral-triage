# Azure Infrastructure Setup Guide: Referral Triage Application

This document provides a comprehensive guide to the Azure resources created for the Referral Triage application, including deployment procedures, configuration details, and upgrade paths to Microsoft Foundry.

## Table of Contents
- [Overview](#overview)
- [Resources Created](#resources-created)
- [Setup Steps](#setup-steps)
- [Deployment Types](#deployment-types)
- [Upgrading to Microsoft Foundry](#upgrading-to-microsoft-foundry)
- [Configuration Reference](#configuration-reference)

---

## Overview

The Referral Triage application uses two primary Azure AI services:
1. **Azure Document Intelligence** - for document analysis and text extraction
2. **Azure OpenAI** - for intelligent triage classification and summarization

These services work together within Azure Functions to process referral documents and provide AI-powered intake routing.

---

## Resources Created

### 1. Azure Document Intelligence
- **Resource Name:** `referral-triage-docint`
- **Endpoint:** `https://referral-triage-docint.cognitiveservices.azure.com/`
- **Purpose:** Extracts text and metadata from uploaded referral documents
- **API Version:** Cognitive Services API
- **Capabilities:**
  - Text extraction from images and PDFs
  - Document analysis and layout understanding
  - Confidence scoring for extracted content

### 2. Azure OpenAI
- **Resource Name:** `referral-triage-openai`
- **Endpoint:** `https://referral-triage-openai.openai.azure.com/`
- **Purpose:** Provides gpt-4o model for intelligent document classification
- **Deployment Name:** `gpt-4o`
- **Deployment Type:** Global Standard (pay-per-token)
- **Capabilities:**
  - Chat completion API
  - Document classification and triage
  - Summarization and content extraction
  - Contextual analysis

---

## Setup Steps

### Step 1: Create Document Intelligence Resource

1. Navigate to the [Azure Portal](https://portal.azure.com)
2. Click **Create a resource**
3. Search for **Document Intelligence** (formerly Form Recognizer)
4. Click **Create**
5. Configure:
   - **Subscription:** Your Azure subscription
   - **Resource Group:** Select or create `referral-triage-rg`
   - **Region:** East US 2 (or your preferred region)
   - **Name:** `referral-triage-docint`
   - **Pricing Tier:** Standard S1 (or Free F0 for testing)
6. Click **Review + Create** → **Create**
7. Wait for deployment to complete
8. Go to **Keys and Endpoint** in the left menu
9. Copy the **Endpoint** and **Key** to your configuration (see [Configuration Reference](#configuration-reference))

### Step 2: Create Azure OpenAI Resource

1. In the Azure Portal, click **Create a resource**
2. Search for **Azure OpenAI**
3. Click **Create**
4. Configure:
   - **Subscription:** Your Azure subscription
   - **Resource Group:** Select `referral-triage-rg`
   - **Region:** East US 2 (important: must be a region where gpt-4o is available)
   - **Name:** `referral-triage-openai`
   - **Pricing Tier:** Standard S0
5. Click **Review + Create** → **Create**
6. Wait for deployment to complete
7. Go to **Keys and Endpoint** in the left menu
8. Copy the **Endpoint** and **Key** to your configuration

### Step 3: Deploy the GPT-4o Model

**This step is critical and must be completed before the application can function.**

1. In your Azure OpenAI resource, click **Model deployments** (under "Resource Management")
2. Click **Create new deployment**
3. Configure:
   - **Model name:** Select `gpt-4o` from the dropdown
   - **Deployment name:** `gpt-4o` (must match exactly what the code expects)
   - **Model version:** Select the latest available version
   - **Deployment type:** Select **Global Standard** (standard pay-per-token billing)
   - **Tokens per minute rate limit (TPM):** Set to at least 40,000
4. Click **Create**
5. **Wait for the deployment status to show "Succeeded"** (this may take 5-10 minutes)

**Verification:**
```bash
# Test the deployment with curl (replace with your values):
curl -H "api-key: YOUR_API_KEY" \
  "https://referral-triage-openai.openai.azure.com/openai/deployments/gpt-4o/chat/completions?api-version=2023-05-15" \
  -H "Content-Type: application/json" \
  -d '{"messages": [{"role": "user", "content": "test"}]}'
```

If you receive an HTTP 200 response with a valid completion, the deployment is working.

### Step 4: Configure Local Settings

Update `src/ReferralTriageApp/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "DocumentIntelligenceEndpoint": "https://referral-triage-docint.cognitiveservices.azure.com/",
    "DocumentIntelligenceKey": "YOUR_DOCUMENT_INTELLIGENCE_API_KEY",
    "AzureOpenAiEndpoint": "https://referral-triage-openai.openai.azure.com/",
    "AzureOpenAiKey": "YOUR_AZURE_OPENAI_API_KEY",
    "AzureOpenAiDeploymentName": "gpt-4o"
  }
}
```

### Step 5: Verify Infrastructure

Run the test program to ensure all services are accessible:

```bash
# From the referral-triage root directory
cd test-openai-app/test
dotnet run
```

Expected output:
```
✓ Azure OpenAI (gpt-4o) test successful!
-----------------------------------------
[Response from gpt-4o model...]
```

---

## Deployment Types

When deploying models in Azure OpenAI or Microsoft Foundry, you must choose a deployment type that determines:
- **Where data is processed** (global, data zone, or single region)
- **How you pay** (pay-per-token or reserved capacity)
- **Performance characteristics** (latency variance, throughput limits)

### Available Deployment Types

| Deployment Type | SKU Name | Billing | Best For | Data Processing |
|-----------------|----------|---------|----------|-----------------|
| **Global Standard** | `GlobalStandard` | Pay-per-token | General workloads, variable traffic | Any Azure region (highest quota) |
| **Global Provisioned** | `GlobalProvisionedManaged` | Reserved PTU | Consistent high-volume workloads | Any Azure region, guaranteed throughput |
| **Global Batch** | `GlobalBatch` | 50% discount | Large async jobs (24-hour turnaround) | Any Azure region |
| **Data Zone Standard** | `DataZoneStandard` | Pay-per-token | EU/US compliance requirement | Within EU or US data zone only |
| **Data Zone Provisioned** | `DataZoneProvisionedManaged` | Reserved PTU | Data zone + predictable throughput | Within EU or US data zone only |
| **Data Zone Batch** | `DataZoneBatch` | 50% discount | Large async jobs with data zone | Within EU or US data zone only |
| **Standard** | `Standard` | Pay-per-token | Low-to-medium volume, regional compliance | Single region only |
| **Regional Provisioned** | `ProvisionedManaged` | Reserved PTU | Regional compliance + high throughput | Single region only |
| **Developer** | `DeveloperTier` | Pay-per-token | Fine-tuned model evaluation only | Single region (24-hour lifetime) |

### Global Standard (Current Configuration)

The referral-triage application uses **Global Standard** deployment type:

**Characteristics:**
- ✅ Pay-per-token billing (lowest startup cost)
- ✅ Available in any Azure region
- ✅ Highest initial quota for tokens per minute (TPM)
- ✅ Best for variable/bursty traffic patterns
- ⚠️ May experience latency variance under high sustained load

**Cost Optimization:**
- For variable traffic: Global Standard is optimal
- For consistent high volume: Consider upgrading to Global Provisioned
- For batch processing: Consider Global Batch (50% cost savings)

### When to Change Deployment Type

You can modify the deployment type in the Azure Portal at any time:

1. Go to your Azure OpenAI resource
2. Click **Model deployments**
3. Click your deployment (e.g., gpt-4o)
4. In the deployment details, you can scale or change the SKU
5. For major changes, consider deleting and redeploying

---

## Upgrading to Microsoft Foundry

Microsoft Foundry is the next evolution of Azure OpenAI, providing access to a broader model catalog, agents service, and advanced evaluation capabilities.

### Benefits of Upgrading to Foundry

| Feature | Azure OpenAI | Microsoft Foundry |
|---------|--------------|-------------------|
| **Models Available** | Azure OpenAI models only | Azure OpenAI + Black Forest Labs, DeepSeek, Meta, xAI, Mistral, Microsoft |
| **Agent Service** | ❌ | ✅ |
| **Foundry API** | ❌ | ✅ |
| **Foundry Tools** | ❌ | ✅ (Speech, Vision, Language, Content Understanding) |
| **Batch Processing** | ✅ | ✅ |
| **Fine-tuning** | ✅ | ✅ |
| **Evaluation Tools** | ❌ | ✅ |
| **Pricing** | Current | No increase for existing Azure OpenAI features |

### Prerequisites for Upgrade

Before upgrading your Azure OpenAI resource to Foundry:

1. **Azure Role Requirements:**
   - You must have **Owner** role on the subscription or resource group
   - This is required for resource management and role assignment

2. **Managed Identity:**
   - System-assigned managed identity must be enabled
   - To enable: Azure Portal → Your Resource → Identity → System assigned → On

3. **Configuration Review:**
   - Check for custom network configurations
   - Review RBAC and Azure Policy settings
   - If using customer-managed encryption keys, submit a request form

### How to Upgrade: Via Azure Portal

1. **Enable Managed Identity** (if not already enabled)
   - Go to your `referral-triage-openai` resource
   - Click **Identity** in the left menu
   - Toggle **System assigned** to **On**
   - Click **Save**

2. **Initiate Upgrade**
   - Go to your Azure OpenAI resource
   - Look for the upgrade banner or notification
   - Click **Upgrade to Foundry** or **Get started**

3. **Create Your First Project**
   - Enter a name for your project (e.g., "referral-triage-project")
   - Your first project is backward compatible with existing Azure OpenAI work
   - Click **Confirm** to start the upgrade

4. **Wait for Completion**
   - The upgrade process typically completes in 2-5 minutes
   - You'll see a confirmation once complete

5. **Verify Upgrade**
   - After upgrade, your resource name and endpoint remain unchanged
   - You'll have access to the Foundry catalog and features
   - Your existing API keys continue to work

### How to Upgrade: Via Azure Bicep (Recommended for Infrastructure-as-Code)

If you're using Infrastructure-as-Code, you can upgrade via Bicep:

```bicep
resource cognitiveServicesAccount 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: 'referral-triage-openai'
  location: 'eastus2'
  kind: 'OpenAI'
  properties: {
    // ... existing properties
    customSubdomainName: 'referral-triage-openai'
  }
  identity: {
    type: 'SystemAssigned'
  }
}
```

Then use Azure CLI to perform the upgrade:
```bash
az cognitiveservices account upgrade-to-foundry \
  --resource-group referral-triage-rg \
  --name referral-triage-openai
```

### What Stays the Same After Upgrade

✅ Resource name: `referral-triage-openai`
✅ API Endpoint: `https://referral-triage-openai.openai.azure.com/`
✅ API Keys: Continue to work
✅ Existing deployments (gpt-4o): Remain unchanged
✅ Existing fine-tuning jobs and state
✅ Network configurations
✅ Azure resource tags
✅ RBAC configuration (continues to function)

### New Capabilities After Upgrade

🆕 **Agent Service** - Build autonomous agents
🆕 **Foundry API** - Access broader model catalog programmatically
🆕 **Foundry Tools** - Speech, Vision, Language, Content Understanding
🆕 **Model Catalog** - Access to Black Forest Labs, DeepSeek, Meta, xAI, Mistral models
🆕 **Evaluation Tools** - Assess model performance systematically
🆕 **Projects** - Organize work and manage access

### Rollback to Azure OpenAI (If Needed)

If you need to revert after upgrading, you can rollback:

1. Delete any non-Azure OpenAI model deployments
2. Delete any Projects or Connections you created
3. Go to **Management Center** in Foundry portal
4. Click **Rollback**

**Important:** You cannot use certain Foundry features after rollback. Plan your rollback before creating Foundry-specific resources.

### Private Network Considerations

If your Azure OpenAI resource is deployed in a private network:

After upgrade to Foundry, you'll need to configure DNS for three FQDNs:
- `{custom-domain}.openai.azure.com`
- `{custom-domain}.services.ai.azure.com`
- `{custom-domain}.cognitiveservices.azure.com`

**Required steps:**
1. Create/update Azure DNS Zones for each FQDN
2. Recreate private link endpoints
3. Verify connectivity before using Foundry features

---

## Configuration Reference

### Environment Variables (local.settings.json)

**⚠️ Important: Never commit this file with real credentials to version control. Use Azure Key Vault or managed identity in production.**

```json
{
  "Values": {
    "DocumentIntelligenceEndpoint": "https://referral-triage-docint.cognitiveservices.azure.com/",
    "DocumentIntelligenceKey": "YOUR_DOCUMENT_INTELLIGENCE_API_KEY",
    "AzureOpenAiEndpoint": "https://referral-triage-openai.openai.azure.com/",
    "AzureOpenAiKey": "YOUR_AZURE_OPENAI_API_KEY",
    "AzureOpenAiDeploymentName": "gpt-4o"
  }
}
```

**To populate these values:**
1. Go to Azure Portal → `referral-triage-docint` resource
2. Click **Keys and Endpoint** → Copy Key 1 or Key 2
3. Replace `YOUR_DOCUMENT_INTELLIGENCE_API_KEY` with the copied value
4. Go to Azure Portal → `referral-triage-openai` resource
5. Click **Keys and Endpoint** → Copy Key 1 or Key 2
6. Replace `YOUR_AZURE_OPENAI_API_KEY` with the copied value

### SDK Configuration (C# Code Example)

```csharp
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using OpenAI.Chat;

// Document Intelligence Setup
var docIntelligenceEndpoint = Environment.GetEnvironmentVariable("DocumentIntelligenceEndpoint");
var docIntelligenceKey = Environment.GetEnvironmentVariable("DocumentIntelligenceKey");
var docClient = new DocumentIntelligenceClient(
    new Uri(docIntelligenceEndpoint),
    new AzureKeyCredential(docIntelligenceKey)
);

// Azure OpenAI Setup
var openAiEndpoint = Environment.GetEnvironmentVariable("AzureOpenAiEndpoint");
var openAiKey = Environment.GetEnvironmentVariable("AzureOpenAiKey");
var deploymentName = Environment.GetEnvironmentVariable("AzureOpenAiDeploymentName");

var azureClient = new AzureOpenAIClient(
    new Uri(openAiEndpoint),
    new AzureKeyCredential(openAiKey)
);
var chatClient = azureClient.GetChatClient(deploymentName);
```

### Quota and Limits

**Azure Document Intelligence (Standard S1):**
- 500 pages per minute (PPM)
- Support for multiple document types
- API version: 2024-02-29-preview or later

**Azure OpenAI (gpt-4o, Global Standard):**
- Tokens per minute (TPM): Starting quota (request increase as needed)
- Max input tokens: 128,000
- Max output tokens: 4,096
- Requests per minute (RPM): Varies by overall quota

### Testing the Configuration

Use the provided test script to verify all services:

```bash
# Navigate to the test project
cd test-openai-app/test

# Restore dependencies
dotnet restore

# Run the test
dotnet run
```

Expected output on success:
```
✓ Azure OpenAI (gpt-4o) test successful!
-----------------------------------------
[Response from gpt-4o to "I am going to Paris, what should I see?"]
```

---

## Next Steps

1. ✅ **Document Intelligence created and verified**
2. ✅ **Azure OpenAI created with gpt-4o deployed**
3. ✅ **Configuration complete (local.settings.json)**
4. ➡️ **Start Azure Functions locally:** `func start`
5. ➡️ *(Optional)* **Upgrade to Microsoft Foundry** (see section above)
6. ➡️ **Deploy to Azure via `azd up`**

---

## Troubleshooting

### "DeploymentNotFound" Error

**Cause:** The gpt-4o model is not deployed or deployment is still in progress

**Solution:**
1. Go to Azure Portal → referral-triage-openai resource
2. Click **Model deployments**
3. Check if gpt-4o deployment status is "Succeeded"
4. If not, wait 5-10 minutes and try again
5. If status shows "Failed", delete and redeploy

### "Unauthorized" Error

**Cause:** Invalid API key or expired credentials

**Solution:**
1. Go to Azure Portal → resource
2. Click **Keys and Endpoint**
3. Copy and update the key in local.settings.json
4. Regenerate key if needed (old key becomes invalid)

### Document Not Extracting

**Cause:** Document Intelligence endpoint unreachable or configuration error

**Solution:**
1. Verify endpoint URL includes trailing slash: `https://referral-triage-docint.cognitiveservices.azure.com/`
2. Test with curl: `curl -H "Ocp-Apim-Subscription-Key: YOUR_KEY" "https://referral-triage-docint.cognitiveservices.azure.com/documentintelligence/?api-version=2024-02-29-preview"`
3. Check Azure Portal for service health status

### "Quota Exceeded" Error

**Cause:** Tokens per minute limit reached

**Solution:**
1. Go to Azure Portal → quotas
2. Request quota increase for Azure OpenAI
3. Add more provisioned throughput units (PTU) if using Foundry
4. Consider switching to Global Batch for non-real-time processing

---

## References

- [Azure Document Intelligence Documentation](https://learn.microsoft.com/azure/ai-services/document-intelligence/)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/azure/ai-services/openai/overview)
- [Microsoft Foundry Deployment Types](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/concepts/deployment-types)
- [Upgrade Azure OpenAI to Foundry](https://learn.microsoft.com/en-us/azure/foundry/how-to/upgrade-azure-openai)
- [Azure OpenAI Quotas and Limits](https://learn.microsoft.com/azure/ai-services/openai/quotas-and-limits)
- [Referral Triage README](./README.md)

---

**Last Updated:** March 14, 2026
**Status:** Infrastructure setup complete and verified
