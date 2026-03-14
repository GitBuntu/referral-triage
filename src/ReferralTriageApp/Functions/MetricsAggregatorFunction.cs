using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Services;

namespace ReferralTriageApp.Functions;

public class MetricsAggregatorFunction
{
    private readonly IMetricsAggregationService _metricsAggregationService;
    private readonly ILogger<MetricsAggregatorFunction> _logger;

    public MetricsAggregatorFunction(
        IMetricsAggregationService metricsAggregationService,
        ILogger<MetricsAggregatorFunction> logger)
    {
        _metricsAggregationService = metricsAggregationService;
        _logger = logger;
    }

    [Function("MetricsAggregator")]
    public async Task Run([TimerTrigger("%AzureServiceSettings:MetricsAggregationSchedule%")] TimerInfo myTimer)
    {
        _logger.LogInformation("MetricsAggregator timer trigger function executed at: {Now}", DateTime.UtcNow);

        try
        {
            // Aggregate metrics for yesterday (previous day)
            var metricsDate = DateTime.UtcNow.AddDays(-1).Date;

            _logger.LogInformation("Starting metrics aggregation for date: {MetricsDate:yyyy-MM-dd}", metricsDate);

            await _metricsAggregationService.AggregateMetricsAsync(metricsDate);

            _logger.LogInformation("Metrics aggregation completed successfully for date: {MetricsDate:yyyy-MM-dd}", metricsDate);

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation(
                    "Next timer schedule: {Next}",
                    myTimer.ScheduleStatus.Next);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during metrics aggregation");
            throw;
        }
    }
}
