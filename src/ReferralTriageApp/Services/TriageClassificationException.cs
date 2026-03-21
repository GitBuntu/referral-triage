using System;

namespace ReferralTriageApp.Services;

/// <summary>
/// Exception thrown when AI-based triage classification fails
/// </summary>
public class TriageClassificationException : Exception
{
    public TriageClassificationException(string message) : base(message)
    {
    }

    public TriageClassificationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
