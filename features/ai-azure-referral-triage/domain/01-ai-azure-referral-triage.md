# Bounded Context: AI-Azure-Referral-Triage

---

## 🔗 SpecForge Chain Position
**This is artefact 1 of 7.**

## 🔗 SpecForge Chain
**Context** → Requirements (1..N) → Feature → Scenario → Test → Plan → Tasks  
↑ You are here

---

## Purpose
**Context Name**: AI-Azure-Referral-Triage

**Responsibility**: This bounded context enforces the business rule: "Referral documents must be ingested, classified, and summarized with consistent accuracy before triage teams process them. Each referral must have exactly one triage record with complete classification data (specialty, urgency, key clinical fields) and must preserve the original document for audit."

**Boundaries**: This context is bounded by:
- Upstream inputs: External referral documents (PDF, text, scanned) received via HTTP intake endpoint
- Downstream outputs: Triage classification events (ReferralTriaged) to downstream clinical workflow contexts; daily metrics aggregation events (MetricsAggregated) to reporting contexts
- Not included: Patient master data management, clinical decision support beyond triage classification, workflow authorization, appointment scheduling, payment processing

## Ubiquitous Language
Each term MUST follow this exact format.  
**Do not add narrative — only add terms that have distinct meaning in this bounded context.**

- **ReferralId**:
  - Meaning: A unique identifier for a single referral intake event
  - Used in: Referral aggregate root
  - Not to be confused with: Patient ID, appointment ID, or case ID
  - Example usage: "ReferralId = UUID('550e8400-e29b-41d4-a716-446655440000')"

- **TriageClassification**:
  - Meaning: The structured output of AI classification containing specialty, urgency, key fields, and summary
  - Used in: TriageRecord entity
  - Not to be confused with: Manual clinical assessment or clinician judgment
  - Example usage: "TriageClassification {specialty: 'cardiology', urgency: 'urgent', symptoms: ['chest pain'], summary: 'Acute presentation'}"

- **Specialty**:
  - Meaning: Medical specialty classification assigned by AI classifier (e.g., cardiology, orthopedics, neurology, dermatology, general medicine)
  - Used in: TriageClassification value object
  - Not to be confused with: Hospital department or clinic assignment
  - Example usage: "specialty = 'cardiology'"

- **Urgency**:
  - Meaning: Triage priority level (routine, soon, urgent) assigned by AI based on clinical presentation
  - Used in: TriageClassification value object
  - Not to be confused with: ER severity codes or critical care assessment
  - Example usage: "urgency = 'urgent' means process within 24 hours"

- **ReferralDocument**:
  - Meaning: The raw ingested referral content (PDF, text, or OCR-extracted text)
  - Used in: Referral aggregate root
  - Not to be confused with: TriageRecord (which is structured output)
  - Example usage: "ReferralDocument.content = PDF file bytes or extracted text"

- **TriageRecord**:
  - Meaning: The immutable structured record produced by triage processing, containing classification and key clinical fields
  - Used in: Referral aggregate root
  - Not to be confused with: The raw referral document or the triage decision
  - Example usage: "TriageRecord {patientName, dob, symptoms, duration, redFlags, specialty, urgency, summary}"

- **ProcessingLatency**:
  - Meaning: Time elapsed (in milliseconds) from document ingestion to triage record completion
  - Used in: DailyMetrics aggregate
  - Not to be confused with: Real-time SLA or clinical urgency
  - Example usage: "ProcessingLatency = 4500ms"

- **Metrics**:
  - Meaning: Daily aggregated statistics on referral processing performance
  - Used in: DailyMetrics aggregate
  - Not to be confused with: Real-time monitoring or individual referral status
  - Example usage: "Metrics { referralsPerSpecialty: {cardiology: 15, orthopedics: 8}, urgentCount: 3, avgLatency: 4200ms, missingFieldRate: 0.05 }"

## Aggregates
Each aggregate MUST follow this exact format.  
**Do not add narrative — only structural facts.**

- **Referral** (Aggregate Root):
  - Responsibility: Enforce that each ingested referral document produces exactly one triage record with complete classification data, and preserve the original document for audit
  - Why boundary: Owns complete lifecycle of a single referral from ingestion to triage completion; atomic transaction boundary for document storage and triage record creation
  - Owned entities: TriageRecord
  - Value objects: ReferralDocument, TriageClassification

- **TriageRecord** (Entity):
  - Responsibility: Represent the immutable structured output of AI triage processing with all required clinical fields
  - Why boundary: Captures the complete state of triage processing for a single referral; owned by Referral aggregate
  - Owned entities: None
  - Value objects: TriageClassification

- **DailyMetrics** (Aggregate Root):
  - Responsibility: Aggregate daily statistics on referral processing performance and track data quality indicators
  - Why boundary: Atomic boundary for daily metrics calculation run; independent of individual referral processing; owns all metrics for a single day
  - Owned entities: None
  - Value objects: Metrics

## Value Objects
Each value object MUST follow this exact format.  
**Do not add narrative — only structural facts.**

- **ReferralDocument**:
  - Immutable attributes: DocumentId: string, FileType: enum(PDF|TEXT|SCANNED), ContentData: bytes, UploadedSource: string (HTTP endpoint URL), UploadTimestamp: datetime
  - Input validation rules: FileType in [PDF, TEXT, SCANNED], ContentData.length > 0, ContentData.length <= 5MB, UploadTimestamp must be valid RFC3339 datetime
  - Equality: Equality by value, not by reference
  - Owned by: Referral aggregate

- **TriageClassification**:
  - Immutable attributes: Specialty: enum(cardiology|orthopedics|neurology|dermatology|generalMedicine), Urgency: enum(routine|soon|urgent), PatientName: string, DateOfBirth: datetime, Symptoms: string[], Duration: string, RedFlags: string[], ClinicalSummary: string, ClassificationTimestamp: datetime
  - Input validation rules: Specialty in [cardiology, orthopedics, neurology, dermatology, generalMedicine], Urgency in [routine, soon, urgent], PatientName.length > 0, PatientName.length <= 100, DateOfBirth must be valid date (not future), Symptoms.length >= 1, Symptoms.length <= 20, Duration.length > 0, Duration.length <= 200, RedFlags.length >= 0, RedFlags.length <= 10, ClinicalSummary.length > 0, ClinicalSummary.length <= 500, ClassificationTimestamp must be valid RFC3339 datetime
  - Equality: Equality by value, not by reference
  - Owned by: TriageRecord entity

- **Metrics**:
  - Immutable attributes: ReferralsPerSpecialtyMap: map(specialty -> count), UrgentCount: int, RoutineCount: int, SoonCount: int, AverageProcessingLatencyMs: decimal, ProcessedCount: int, MissingFieldRate: decimal, MetricsDate: date, AggregationTimestamp: datetime
  - Input validation rules: All counts >= 0, AverageProcessingLatencyMs >= 0, ProcessedCount >= 0, MissingFieldRate in [0.0, 1.0], ReferralsPerSpecialtyMap keys in [cardiology, orthopedics, neurology, dermatology, generalMedicine], MetricsDate is valid date, AggregationTimestamp must be valid RFC3339 datetime, UrgentCount + SoonCount + RoutineCount > 0
  - Equality: Equality by value, not by reference
  - Owned by: DailyMetrics aggregate

## Domain Services
Each domain service MUST follow this exact format.  
**Do not add narrative — only structural facts. Domain services are rare; justify why the logic cannot live in an aggregate.**

- **AITriageClassifier**:
  - Logic: Extract text from referral document and invoke AI model to classify specialty, assign urgency, extract key fields, and generate clinical summary
  - Why not in aggregate: Invokes external AI/ML service (Azure OpenAI or Document Intelligence); requires orchestration of multiple external calls; stateless operation that transforms raw document into structured classification
  - Input: ReferralDocument (with ContentData), ReferralId
  - Output: TriageClassification value object
  - Called by: Referral aggregate (via process trigger)

- **TextExtractor**:
  - Logic: Extract text from PDF or scanned document using OCR if needed
  - Why not in aggregate: Requires external OCR service (Azure Document Intelligence); stateless transformation of file format
  - Input: ReferralDocument
  - Output: ExtractedText: string
  - Called by: AITriageClassifier domain service

- **MetricsCalculator**:
  - Logic: Aggregate referral data for a given day and compute statistics (per-specialty counts, urgency distribution, average latency, missing-field rate)
  - Why not in aggregate: Aggregates data across many Referral aggregates by querying repository; compute-heavy operation
  - Input: ProcessedReferrals: list of Referral aggregates for a specific date
  - Output: Metrics value object
  - Called by: DailyMetrics aggregate (via timer trigger)

## Invariants
Each invariant MUST follow this exact format.  
**Do not add interpretive prose — only add invariants that can be mechanically enforced.**

- **[Referral] Invariant**: Each Referral must have exactly one TriageRecord after successful triage processing completes.
  - Measurable form: Referral.TriageRecord != null AND Referral.TriageRecord.Id is unique within context
  - Example violation: Referral created, document ingested, but no TriageRecord created after AI processing
  - Impact: Triage teams receive incomplete referrals; audit trail is compromised
  - Enforced by: Referral aggregate root rejects state transitions that would create duplicate or missing TriageRecord

- **[Referral] Invariant**: ReferralDocument must be preserved in original form for audit, separate from TriageRecord.
  - Measurable form: Referral.RawDocument.ContentData equals original uploaded bytes; Referral.TriageRecord contains only structured classification (no raw document data)
  - Example violation: Original PDF deleted after triage processing; only TriageRecord retained
  - Impact: No audit trail for clinician review of original referral; regulatory compliance violations
  - Enforced by: Referral aggregate root stores both ReferralDocument and TriageRecord as independent owned entities

- **[TriageClassification] Invariant**: All required clinical fields must be populated (not null, not empty) after AI classification.
  - Measurable form: TriageClassification.PatientName != null && TriageClassification.PatientName.length > 0 AND TriageClassification.Specialty in [cardiology, orthopedics, neurology, dermatology, generalMedicine] AND TriageClassification.Urgency in [routine, soon, urgent] AND TriageClassification.Symptoms.length >= 1 AND TriageClassification.ClinicalSummary.length > 0
  - Example violation: Specialty is null, or Symptoms list is empty, or ClinicalSummary is blank
  - Impact: Incomplete triage information delays clinical decision-making; missing data causes workflow failures
  - Enforced by: TriageClassification value object validation rules at construction time

- **[DailyMetrics] Invariant**: Daily metrics must be based only on referrals processed within the calendar day (00:00-23:59 UTC).
  - Measurable form: All Referral.TriageRecord.ClassificationTimestamp values in Metrics calculation must have MetricsDate as date portion
  - Example violation: Including referrals from previous day (23:59:59 yesterday) in today's metrics; including referrals from next day
  - Impact: Metrics are inaccurate; reporting dashboards show misleading data across day boundaries
  - Enforced by: MetricsCalculator domain service filters by exact date before aggregation

## Repositories
Each repository MUST follow this exact format.  
**Do NOT describe infrastructure (SQL, EF, API) — only domain query patterns and lifecycle.**

- **Referral**:
  - Interface: IReferralRepository
  - Load patterns: by ReferralId (unique), by status (pending, processed, failed), all processed referrals for a date range
  - Save lifecycle: on creation (document ingested), on state change (TriageRecord added), on completion (triage processing succeeded)

- **DailyMetrics**:
  - Interface: IDailyMetricsRepository
  - Load patterns: by date, by date range
  - Save lifecycle: on daily aggregation completion

## Domain Events
Each domain event MUST follow this exact format.  
**Events are ONLY emitted when an invariant is preserved and a state boundary is crossed.**

- **ReferralReceived** (Pattern: [Entity][Action]Occurred):
  - Examples: "ReferralReceived"
  - Emitted by: Referral aggregate root
  - Trigger: Referral aggregate is created when HTTP endpoint validates and accepts a valid referral document
  - Payload: {ReferralId: UUID, DocumentId: UUID, FileType: enum, UploadTimestamp: datetime, UploadSource: string}
  - Invariants preserved: Invariant "Each Referral must have exactly one TriageRecord" (initially satisfied: Referral created, awaiting TriageRecord)
  - Consequence: Triggers Blob Storage upload of ReferralDocument; triggers AI Triage Processor function via Blob trigger

- **ReferralTriaged** (Pattern: [Entity][StateChange]Occurred):
  - Examples: "ReferralTriaged"
  - Emitted by: Referral aggregate root
  - Trigger: TriageRecord is successfully created and assigned to Referral aggregate after AI classification completes
  - Payload: {ReferralId: UUID, TriageRecordId: UUID, Specialty: enum, Urgency: enum, PatientName: string, DateOfBirth: datetime, Symptoms: string[], Duration: string, RedFlags: string[], ClinicalSummary: string, ClassificationTimestamp: datetime, ProcessingLatencyMs: int}
  - Invariants preserved: "Each Referral must have exactly one TriageRecord" (now satisfied: TriageRecord assigned), "ReferralDocument preserved for audit" (original document still in Referral), "All required clinical fields populated" (TriageClassification validated)
  - Consequence: Downstream clinical workflow context receives triage classification and starts next processing stage (specialty-specific triage workqueue); DailyMetrics context subscribes to calculate latency and collect for daily aggregation

- **MetricsAggregated** (Pattern: [Entity][Action]Occurred):
  - Examples: "MetricsAggregated"
  - Emitted by: DailyMetrics aggregate root
  - Trigger: Daily metrics calculation runs at 02:00 UTC and completes aggregation of all referrals processed in the previous calendar day
  - Payload: {MetricsDate: date, ReferralsPerSpecialty: map(specialty -> count), UrgentCount: int, RoutineCount: int, SoonCount: int, AverageProcessingLatencyMs: decimal, ProcessedCount: int, MissingFieldRate: decimal, AggregationTimestamp: datetime}
  - Invariants preserved: "Daily metrics based only on referrals within calendar day" (timestamp filtering enforced)
  - Consequence: Reporting/analytics context receives daily metrics for dashboard and KPI calculations; operations teams use metrics for performance monitoring and SLA reporting

---

## SpecForge Rules
- This file defines the ONLY valid aggregates, value objects, invariants, and domain events.
- No other artefact may introduce new aggregates, value objects, invariants, or events.
- Aggregate roots are the ONLY entry points for modifying data within this context.
- Value objects are immutable and must be validated at creation.

---

## Next Step Directive
Create one or more requirements using the `REQ-NNN.md` template.  
Each requirement **must reference at least one invariant and one domain event** defined above.
