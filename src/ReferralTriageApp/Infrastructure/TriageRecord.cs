using System;
using System.Collections.Generic;

namespace ReferralTriageApp.Infrastructure;

public partial class TriageRecord
{
    public Guid TriageRecordId { get; set; }

    public Guid ReferralId { get; set; }

    public string Specialty { get; set; } = null!;

    public string Urgency { get; set; } = null!;

    public string ExtractedFields { get; set; } = null!;

    public string ClinicalSummary { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime TriagedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public virtual Referral Referral { get; set; } = null!;
}
