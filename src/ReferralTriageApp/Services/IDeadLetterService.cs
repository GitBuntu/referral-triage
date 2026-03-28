using System.Threading.Tasks;

namespace ReferralTriageApp.Services;

/// <summary>
/// Interface for emitting failed referrals to a dead-letter queue
/// </summary>
public interface IDeadLetterService
{
    /// <summary>
    /// Emits a failed referral to the dead-letter queue for manual review
    /// </summary>
    /// <param name="referralId">The ID of the failed referral</param>
    /// <param name="failureReason">Reason for the failure (e.g., "document_extraction_failed", "classification_failed")</param>
    /// <param name="errorMessage">Detailed error message</param>
    /// <param name="retryCount">Number of retry attempts made before failure</param>
    /// <returns>Task representing the async operation</returns>
    Task EmitToDeadLetterAsync(string referralId, string failureReason, string errorMessage, int retryCount);
}
