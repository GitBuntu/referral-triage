using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Models;
using ReferralTriageApp.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ReferralTriageApp.Services;

public class MetricsAggregationService : IMetricsAggregationService
{
    private readonly ReferralTriageContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetricsAggregationService> _logger;

    public MetricsAggregationService(
        ReferralTriageContext dbContext,
        IConfiguration configuration,
        ILogger<MetricsAggregationService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task AggregateMetricsAsync(DateTime metricsDate)
    {
        try
        {
            _logger.LogInformation("Starting daily metrics aggregation for date: {MetricsDate:yyyy-MM-dd}", metricsDate);

            // Query triage records from SQL Server
            var startOfDay = metricsDate.Date;
            var endOfDay = startOfDay.AddDays(1);

            var triageRecords = await _dbContext.TriageRecords
                .Where(r => r.CreatedAt >= startOfDay && r.CreatedAt < endOfDay)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} triage records for metrics aggregation", triageRecords.Count);

            // Calculate metrics
            var metrics = new DailyMetrics
            {
                Id = $"metrics-{metricsDate:yyyy-MM-dd}",
                MetricDate = metricsDate,
                TotalReferralsProcessed = triageRecords.Count,
                RoutineCount = triageRecords.Count(r => r.Urgency == "routine"),
                SoonCount = triageRecords.Count(r => r.Urgency == "soon"),
                UrgentCount = triageRecords.Count(r => r.Urgency == "urgent"),
                ReferralsBySpecialty = new Dictionary<string, int>(),
                MissingFieldRates = new Dictionary<string, double>()
            };

            // Count by specialty
            var specialtyCounts = triageRecords
                .GroupBy(r => r.Specialty)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var specialty in specialtyCounts)
            {
                metrics.ReferralsBySpecialty[specialty.Key] = specialty.Value;
            }

            // Calculate missing field rates
            metrics.MissingFieldRates = CalculateMissingFieldRates(triageRecords);

            // Calculate average processing latency
            metrics.AverageProcessingLatencyMs = CalculateAverageLatency(triageRecords);

            // Store metrics in database
            await StoreMetricsAsync(metrics);

            _logger.LogInformation(
                "Metrics aggregation completed. Total: {Total}, Urgent: {Urgent}, Soon: {Soon}, Routine: {Routine}",
                metrics.TotalReferralsProcessed,
                metrics.UrgentCount,
                metrics.SoonCount,
                metrics.RoutineCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating metrics for date: {MetricsDate}", metricsDate);
            throw;
        }
    }

    private Dictionary<string, double> CalculateMissingFieldRates(List<Infrastructure.TriageRecord> records)
    {
        if (records.Count == 0)
            return new Dictionary<string, double>();

        var requiredFields = new[] { "patient_name", "dob", "symptoms", "duration", "red_flags" };
        var missingRates = new Dictionary<string, double>();

        foreach (var field in requiredFields)
        {
            var missingCount = records.Count(r =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(r.ExtractedFields))
                        return true;

                    var extractedFields = System.Text.Json.JsonDocument.Parse(r.ExtractedFields).RootElement;
                    if (!extractedFields.TryGetProperty(field, out var fieldValue))
                        return true;

                    var fieldStr = fieldValue.GetString();
                    return string.IsNullOrWhiteSpace(fieldStr);
                }
                catch
                {
                    return true;
                }
            });

            var rate = (double)missingCount / records.Count;
            missingRates[field] = Math.Round(rate, 4);
        }

        return missingRates;
    }

    private double CalculateAverageLatency(List<Infrastructure.TriageRecord> records)
    {
        if (records.Count == 0)
            return 0;

        // Calculate latency from TriagedAt - CreatedAt
        var latencies = records
            .Select(r => (r.TriagedAt - r.CreatedAt).TotalMilliseconds)
            .Where(ms => ms >= 0)
            .ToList();

        return latencies.Count > 0 ? latencies.Average() : 0;
    }

    private async Task StoreMetricsAsync(DailyMetrics metrics)
    {
        try
        {
            // Store metrics as a domain event log
            var eventPayload = System.Text.Json.JsonSerializer.Serialize(metrics);
            var domainEvent = new DomainEventLog
            {
                DomainEventId = Guid.NewGuid(),
                EventType = "MetricsAggregated",
                ReferralId = Guid.Empty,
                Payload = eventPayload,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.DomainEventLogs.Add(domainEvent);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Metrics stored in SQL Server: {MetricsId}", metrics.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing metrics");
            throw;
        }
    }
}
