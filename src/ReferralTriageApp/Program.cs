using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using ReferralTriageApp.Services;
using ReferralTriageApp.Infrastructure;
using Microsoft.EntityFrameworkCore;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, configBuilder) =>
    {
        configBuilder
            .AddEnvironmentVariables()
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        // Register configuration
        services.AddSingleton(config);

        // Add Azure Monitor/Application Insights
        services.AddOpenTelemetry().UseAzureMonitor();

        // Register Azure Storage Blob client
        var blobConnectionString = config.GetConnectionString("BlobStorage")
            ?? "UseDevelopmentStorage=true";
        services.AddSingleton(new BlobServiceClient(blobConnectionString));

        // Register Azure Storage Queue client for dead-letter queue
        var queueConnectionString = config.GetConnectionString("BlobStorage")
            ?? "UseDevelopmentStorage=true";
        services.AddSingleton(new QueueServiceClient(queueConnectionString));

        // Register Entity Framework Core DbContext
        var sqlConnectionString = config.GetConnectionString("SqlServer")
            ?? "Server=localhost;Database=ReferralTriage;Integrated Security=true;TrustServerCertificate=true;";
        services.AddDbContext<ReferralTriageContext>(options =>
            options.UseSqlServer(sqlConnectionString));

        // Register application services
        services.AddScoped<IReferralIntakeService, ReferralIntakeService>();
        services.AddScoped<ITriageProcessingService, TriageProcessingService>();
        services.AddScoped<IMetricsAggregationService, MetricsAggregationService>();
        services.AddScoped<IDocumentExtractionService, DocumentExtractionService>();
        services.AddScoped<ITriageClassificationService, TriageClassificationService>();
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IDeadLetterService, DeadLetterService>();

        // Register configuration options
        services.Configure<ReferralTriageSettings>(config.GetSection("ReferralTriageApp"));

        // Add logging
        services.AddLogging();
    })
    .Build();

host.Run();
