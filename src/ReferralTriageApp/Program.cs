using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Storage.Blobs;
using ReferralTriageApp.Services;
using ReferralTriageApp.Infrastructure;
using Microsoft.EntityFrameworkCore;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        // Register configuration
        services.AddSingleton(config);

        // Register Azure Storage Blob client
        var blobConnectionString = config.GetConnectionString("BlobStorage")
            ?? "UseDevelopmentStorage=true";
        services.AddSingleton(new BlobServiceClient(blobConnectionString));

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

        // Register configuration options
        services.Configure<AzureServiceSettings>(config.GetSection("AzureServiceSettings"));

        // Add logging
        services.AddLogging();
    })
    .Build();

host.Run();
