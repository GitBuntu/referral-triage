using System;
using System.Collections.Generic;

namespace ReferralTriageApp.Infrastructure;

public partial class SchemaVersions
{
    public int Id { get; set; }

    public string ScriptName { get; set; } = null!;

    public DateTime Applied { get; set; }
}
