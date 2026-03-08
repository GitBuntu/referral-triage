using System;
using System.Collections.Generic;

namespace ReferralTriageApp.Infrastructure;

public partial class DomainEventLog
{
    public Guid DomainEventId { get; set; }

    public string EventType { get; set; } = null!;

    public Guid ReferralId { get; set; }

    public string Payload { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
