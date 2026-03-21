using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using ReferralTriageApp.Services;
using Xunit;

namespace ReferralTriageApp.Tests;

/// <summary>
/// Pragmatic unit tests for RetryHelper exponential backoff logic.
/// Focus: Retry mechanics, failure handling, timing validation.
/// </summary>
public class RetryHelperTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    [Fact]
    public async Task RetryAsync_SuccessOnFirstAttempt_ReturnsValueImmediately()
    {
        // Arrange
        var expectedValue = "test-success";
        Func<Task<string>> operation = () => Task.FromResult(expectedValue);

        // Act
        var result = await RetryHelper.RetryAsync(operation, maxRetries: 2, _mockLogger.Object, "test");

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task RetryAsync_SuccessOnRetry_ReturnsValueAfterFailure()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("Fail first attempt");
            await Task.Delay(10);
            return "success";
        };

        // Act
        var result = await RetryHelper.RetryAsync(operation, maxRetries: 2, _mockLogger.Object, "test");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task RetryAsync_ExhaustedRetries_ReturnsNull()
    {
        // Arrange
        Func<Task<string>> operation = () =>
            throw new InvalidOperationException("Always fails");

        // Act
        var result = await RetryHelper.RetryAsync(operation, maxRetries: 2, _mockLogger.Object, "test");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RetryAsync_Void_SuccessOnFirstAttempt_ReturnsTrue()
    {
        // Arrange
        Func<Task> operation = () => Task.CompletedTask;

        // Act
        var result = await RetryHelper.RetryAsync(operation, maxRetries: 2, _mockLogger.Object, "test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RetryAsync_Void_ExhaustedRetries_ReturnsFalse()
    {
        // Arrange
        Func<Task> operation = () =>
            throw new InvalidOperationException("Always fails");

        // Act
        var result = await RetryHelper.RetryAsync(operation, maxRetries: 2, _mockLogger.Object, "test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RetryAsync_ExponentialBackoff_IncreasingDelay()
    {
        // Arrange
        var times = new List<long>();
        Func<Task<string>> operation = async () =>
        {
            times.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (times.Count < 3)
                throw new InvalidOperationException("Retry");
            return await Task.FromResult("success");
        };

        // Act
        var result = await RetryHelper.RetryAsync(operation, maxRetries: 3, _mockLogger.Object, "test");

        // Assert
        Assert.Equal("success", result);

        // Delay 1 should be ~1000ms, Delay 2 should be ~2000ms (exponential backoff)
        if (times.Count >= 2)
        {
            var delay1 = times[1] - times[0];
            Assert.InRange(delay1, 900, 1200); // ~1000ms
        }
        if (times.Count >= 3)
        {
            var delay2 = times[2] - times[1];
            Assert.InRange(delay2, 1900, 2200); // ~2000ms
        }
    }
}

