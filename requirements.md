Epic: AI‑Driven Referral Intake & Triage Pipeline (Azure Functions MVP)

Story: Automated Referral Ingestion and AI‑Based Triage Classification
Story ID: HCARE‑INTAKE‑001
Type: User Story
Status: Draft
Priority: High
Story Points: 5

User Statement
As a clinical operations coordinator, I want incoming referral documents to be automatically ingested, classified, and summarised so that triage teams can process referrals faster and with fewer manual steps.

Business Value
Reduces manual triage workload.

Improves referral turnaround time.

Ensures consistent classification and urgency scoring.

Creates structured data for downstream clinical workflows.

Scope
This story covers the creation of a minimal Azure Functions–based pipeline that ingests referral documents, triggers an AI model for classification and summarisation, and stores structured triage results.

Functional Requirements
1. Referral Intake (HTTP Trigger)
Accept PDF, text, or scanned referral documents via HTTP POST.

Validate file type and size.

Store raw document in Blob Storage under /referrals/incoming/{referralId}.

Return a unique referral ID to the caller.

2. AI Triage Processor (Blob Trigger)
Trigger when a new referral document is added to the incoming container.

Extract text from the document (OCR if needed).

Send extracted text to an AI model to:

Classify specialty (e.g., cardiology, orthopaedics, neurology, dermatology, general medicine).

Assign urgency (routine, soon, urgent).

Extract key fields (patient name, DOB, symptoms, duration, red flags).

Generate a short clinical summary.

Produce a structured JSON triage record.

Store the triage record in Cosmos DB or Table Storage.

3. Daily Metrics Aggregation (Timer Trigger)
Run once per day at 02:00.

Aggregate:

Number of referrals per specialty.

Urgent vs routine counts.

Average processing latency.

Missing‑field rates.

Store metrics in a /metrics/daily table.