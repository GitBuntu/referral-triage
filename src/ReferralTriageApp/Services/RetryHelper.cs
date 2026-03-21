using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ReferralTriageApp.Services;

/// <summary>
/// Generic retry utility for handling transient failures with exponential backoff
/// </summary>
public static class RetryHelper
{
    // Exponential backoff constants
    private const int INITIAL_DELAY_MS = 1000;
    private const int BACKOFF_MULTIPLIER = 2;

    /// <summary>
    /// Retries an async operation with exponential backoff on failure
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="operation">The async operation to retry</param>
    /// <param name="maxRetries">Maximum number of retry attempts (not including initial attempt)</param>
    /// <param name="logger">Logger for retry attempts</param>
    /// <param name="operationName">Name of the operation for logging purposes</param>
    /// <returns>The operation result, or null if all retries fail</returns>
    public static async Task<T?> RetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries,
        ILogger logger,
        string operationName)
        where T : class
    {
        int attemptNumber = 1;

        while (true)
        {
            try
            {
                logger.LogInformation("Attempting {OperationName} (attempt {AttemptNumber})", operationName, attemptNumber);
                var result = await operation();

                if (attemptNumber > 1)
                {
                    logger.LogInformation("{OperationName} succeeded on attempt {AttemptNumber}", operationName, attemptNumber);
                }

                return result;
            }
            catch (Exception ex)
            {
                if (attemptNumber > maxRetries)
                {
                    logger.LogError(
                        ex,
                        "{OperationName} failed after {AttemptCount} attempts. Exception: {ExceptionType}: {ExceptionMessage}",
                        operationName,
                        attemptNumber,
                        ex.GetType().Name,
                        ex.Message);

                    return null;
                }

                // Calculate exponential backoff delay
                int delayMs = INITIAL_DELAY_MS * (int)Math.Pow(BACKOFF_MULTIPLIER, attemptNumber - 1);

                logger.LogWarning(
                    ex,
                    "{OperationName} attempt {AttemptNumber} failed with {ExceptionType}: {ExceptionMessage}. Retrying in {DelayMs}ms",
                    operationName,
                    attemptNumber,
                    ex.GetType().Name,
                    ex.Message,
                    delayMs);

                // Wait before retrying
                await Task.Delay(delayMs);
                attemptNumber++;
            }
        }
    }

    /// <summary>
    /// Retries an async operation that returns void (fire-and-forget pattern) with exponential backoff
    /// </summary>
    /// <param name="operation">The async operation to retry</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="logger">Logger for retry attempts</param>
    /// <param name="operationName">Name of the operation for logging purposes</param>
    /// <returns>True if succeeded, false if all retries failed</returns>
    public static async Task<bool> RetryAsync(
        Func<Task> operation,
        int maxRetries,
        ILogger logger,
        string operationName)
    {
        int attemptNumber = 1;

        while (true)
        {
            try
            {
                logger.LogInformation("Attempting {OperationName} (attempt {AttemptNumber})", operationName, attemptNumber);
                await operation();

                if (attemptNumber > 1)
                {
                    logger.LogInformation("{OperationName} succeeded on attempt {AttemptNumber}", operationName, attemptNumber);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (attemptNumber > maxRetries)
                {
                    logger.LogError(
                        ex,
                        "{OperationName} failed after {AttemptCount} attempts. Exception: {ExceptionType}: {ExceptionMessage}",
                        operationName,
                        attemptNumber,
                        ex.GetType().Name,
                        ex.Message);

                    return false;
                }

                // Calculate exponential backoff delay
                int delayMs = INITIAL_DELAY_MS * (int)Math.Pow(BACKOFF_MULTIPLIER, attemptNumber - 1);

                logger.LogWarning(
                    ex,
                    "{OperationName} attempt {AttemptNumber} failed with {ExceptionType}: {ExceptionMessage}. Retrying in {DelayMs}ms",
                    operationName,
                    attemptNumber,
                    ex.GetType().Name,
                    ex.Message,
                    delayMs);

                // Wait before retrying
                await Task.Delay(delayMs);
                attemptNumber++;
            }
        }
    }
}
