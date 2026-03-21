using System;
using System.Collections.Generic;

namespace ReferralTriageApp.Infrastructure;

public partial class Users
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }
}
