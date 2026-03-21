using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ReferralTriageApp.Services;

/// <summary>
/// Service for emitting failed referrals to Azure Storage Queue dead-letter queue
/// </summary>
public class DeadLetterService : IDeadLetterService
{
    private readonly QueueServiceClient _queueServiceClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeadLetterService> _logger;
    private QueueClient? _queueClient;
    private bool _queueInitialized = false;
    private readonly object _lockObject = new();

    public DeadLetterService(
        QueueServiceClient queueServiceClient,
        IConfiguration configuration,
        ILogger<DeadLetterService> logger)
    {
        _queueServiceClient = queueServiceClient;
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<QueueClient> EnsureQueueInitializedAsync()
    {
        lock (_lockObject)
        {
            if (_queueInitialized && _queueClient != null)
            {
                return _queueClient;
            }
        }

        var dlqName = _configuration["ReferralTriageApp:DLQName"] ?? "referral-dlq";
        _queueClient = _queueServiceClient.GetQueueClient(dlqName);
        await _queueClient.CreateIfNotExistsAsync();

        lock (_lockObject)
        {
            _queueInitialized = true;
        }

        _logger.LogInformation("Dead-letter queue initialized: {QueueName}", dlqName);
        return _queueClient;
    }

    public async Task EmitToDeadLetterAsync(string referralId, string failureReason, string errorMessage, int retryCount)
    {
        try
        {
            var queueClient = await EnsureQueueInitializedAsync();

            // Create the dead-letter message
            var dlqMessage = new
            {
                referralId,
                failureReason,
                errorMessage,
                timestamp = DateTime.UtcNow,
                retryCount
            };

            var messageJson = JsonSerializer.Serialize(dlqMessage);

            // Send to dead-letter queue
            await queueClient.SendMessageAsync(messageJson);

            _logger.LogWarning(
                "Emitted referral {ReferralId} to dead-letter queue. Reason: {FailureReason}, Retries: {RetryCount}",
                referralId,
                failureReason,
                retryCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error emitting referral {ReferralId} to dead-letter queue. Reason: {FailureReason}. Exception: {ExceptionMessage}",
                referralId,
                failureReason,
                ex.Message);

            throw;
        }
    }
}
