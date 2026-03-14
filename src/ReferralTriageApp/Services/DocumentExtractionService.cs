using System;
using System.IO;
using System.Text;
using Azure.AI.DocumentIntelligence;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Models;

namespace ReferralTriageApp.Services;

public class DocumentExtractionService : IDocumentExtractionService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentExtractionService> _logger;

    public DocumentExtractionService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<DocumentExtractionService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> ExtractTextFromDocumentAsync(string blobPath, string documentFormat)
    {
        try
        {
            _logger.LogInformation("Starting text extraction for document: {BlobPath}", blobPath);

            // For text files, just download and return
            if (documentFormat.Equals("txt", StringComparison.OrdinalIgnoreCase) ||
                documentFormat.Equals("text", StringComparison.OrdinalIgnoreCase))
            {
                return await ExtractTextFromTextFileAsync(blobPath);
            }

            // For PDF and images, use Document Intelligence API
            var endpoint = _configuration["AzureServiceSettings:DocumentIntelligenceEndpoint"];
            var key = _configuration["AzureServiceSettings:DocumentIntelligenceKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Document Intelligence credentials not found, falling back to text extraction");
                return await ExtractTextFromBlobAsync(blobPath);
            }

            var client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(key));

            // Get blob client and generate SAS URI (for private containers)
            var containerName = _configuration["AzureServiceSettings:BlobContainer"] ?? "referrals";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            // Generate a short-lived read-only SAS URI (valid for 1 hour)
            var sasUri = GenerateBlobSasUri(blobClient);

            // Analyze document using SAS URI (works with private containers)
            var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", sasUri);

            var sb = new StringBuilder();

            // Extract text from document
            if (operation.Value.Pages != null)
            {
                foreach (var page in operation.Value.Pages)
                {
                    if (page.Lines != null)
                    {
                        foreach (var line in page.Lines)
                        {
                            sb.AppendLine(line.Content);
                        }
                    }
                }
            }

            var extractedText = sb.ToString();
            _logger.LogInformation("Text extraction completed successfully. Extracted length: {Length}", extractedText.Length);

            return extractedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from document: {BlobPath}", blobPath);
            // Fallback to basic text extraction
            return await ExtractTextFromBlobAsync(blobPath);
        }
    }

    private async Task<string> ExtractTextFromTextFileAsync(string blobPath)
    {
        try
        {
            var containerName = _configuration["AzureServiceSettings:BlobContainer"] ?? "referrals";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            var download = await blobClient.DownloadAsync();
            using (var reader = new StreamReader(download.Value.Content))
            {
                return await reader.ReadToEndAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from text file: {BlobPath}", blobPath);
            return string.Empty;
        }
    }

    private async Task<string> ExtractTextFromBlobAsync(string blobPath)
    {
        try
        {
            var containerName = _configuration["AzureServiceSettings:BlobContainer"] ?? "referrals";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            var download = await blobClient.DownloadAsync();
            using (var reader = new StreamReader(download.Value.Content, Encoding.UTF8, false))
            {
                return await reader.ReadToEndAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from blob: {BlobPath}", blobPath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Generates a short-lived read-only SAS URI for the blob.
    /// This is required for Document Intelligence to access private containers.
    /// </summary>
    private Uri GenerateBlobSasUri(BlobClient blobClient)
    {
        try
        {
            // Check if the blob client supports SAS generation (requires storage account key or connection string)
            if (blobClient.CanGenerateSasUri)
            {
                // Create SAS policy: read-only, valid for 1 hour
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b", // "b" = blob
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };

                // Grant read permission only
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                // Generate the SAS URI
                Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
                _logger.LogInformation("Generated SAS URI for blob: {BlobPath}", blobClient.Name);
                return sasUri;
            }
            else
            {
                // Fallback if SAS cannot be generated (e.g., using managed identity without storage key)
                // In this case, Document Intelligence won't be able to access private containers
                _logger.LogWarning("Cannot generate SAS URI. Blob client requires storage account key. Using plain URI (may fail for private containers).");
                return blobClient.Uri;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAS URI for blob: {BlobPath}", blobClient.Name);
            // Fallback to plain URI (will likely fail for private containers)
            return blobClient.Uri;
        }
    }
}
