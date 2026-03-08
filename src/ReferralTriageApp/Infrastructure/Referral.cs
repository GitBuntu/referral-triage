using System;
using System.Collections.Generic;

namespace ReferralTriageApp.Infrastructure;

public partial class Referral
{
    public Guid ReferralId { get; set; }

    public string DocumentFormat { get; set; } = null!;

    public int DocumentSize { get; set; }

    public string DocumentStoragePath { get; set; } = null!;

    public string DocumentHash { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string SubmittedBy { get; set; } = null!;

    public DateTime SubmittedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public virtual TriageRecord? TriageRecord { get; set; }
}
