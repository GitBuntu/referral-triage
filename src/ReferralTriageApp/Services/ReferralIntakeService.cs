using System;
using System.Text;
using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Models;
using ReferralTriageApp.Infrastructure;

namespace ReferralTriageApp.Services;

public class ReferralIntakeService : IReferralIntakeService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ReferralTriageContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IValidationService _validationService;
    private readonly ILogger<ReferralIntakeService> _logger;

    public ReferralIntakeService(
        BlobServiceClient blobServiceClient,
        ReferralTriageContext dbContext,
        IConfiguration configuration,
        IValidationService validationService,
        ILogger<ReferralIntakeService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _dbContext = dbContext;
        _configuration = configuration;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ReferralIntakeResponse> ProcessReferralAsync(ReferralIntakeRequest request)
    {
        try
        {
            // Validate request
            var (isValid, errors) = _validationService.ValidateReferralIntakeRequest(request);
            if (!isValid)
            {
                _logger.LogWarning("Referral intake validation failed: {Errors}", string.Join(", ", errors));
                throw new ArgumentException($"Validation failed: {string.Join(", ", errors)}");
            }

            // Generate unique ReferralId
            var referralId = Guid.NewGuid().ToString();
            _logger.LogInformation("Processing referral intake with ID: {ReferralId}", referralId);

            // Calculate document hash
            var documentBytes = Convert.FromBase64String(request.DocumentData);
            var documentHash = CalculateSHA256Hash(documentBytes);

            // Upload to Blob Storage
            var blobIncomingPath = _configuration["ReferralTriageSettings:BlobIncomingPath"] ?? "incoming";
            var blobPath = $"{blobIncomingPath}/{referralId}/{referralId}.{GetFileExtension(request.DocumentFormat)}";
            var blobUri = await UploadDocumentToBlobAsync(documentBytes, blobPath);

            // Store referral metadata in SQL DB
            var referralDocument = new ReferralDocument
            {
                Id = referralId,
                DocumentFormat = request.DocumentFormat,
                BlobPath = blobPath,
                DocumentHash = documentHash,
                SubmittedAt = DateTime.UtcNow,
                PatientMRN = request.PatientMRN,
                Status = "pending"
            };

            await StoreReferralMetadataAsync(referralDocument, documentBytes.Length);

            return new ReferralIntakeResponse
            {
                ReferralId = referralId,
                BlobUri = blobUri?.ToString(),
                SubmittedAt = referralDocument.SubmittedAt,
                DocumentFormat = request.DocumentFormat,
                DocumentHash = documentHash,
                Message = "Referral successfully submitted and queued for triage processing"
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Validation error in referral intake");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing referral intake");
            throw;
        }
    }

    private async Task<Uri?> UploadDocumentToBlobAsync(byte[] documentBytes, string blobPath)
    {
        try
        {
            var containerName = _configuration["ReferralTriageSettings:BlobContainer"] ?? "referrals";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobPath);

            using (var stream = new MemoryStream(documentBytes))
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            _logger.LogInformation("Document uploaded to blob: {BlobPath}", blobPath);
            return blobClient.Uri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document to blob storage");
            throw;
        }
    }

    private async Task StoreReferralMetadataAsync(ReferralDocument document, int documentSize)
    {
        try
        {
            var referral = new Referral
            {
                ReferralId = Guid.Parse(document.Id),
                DocumentFormat = document.DocumentFormat,
                DocumentSize = documentSize,
                DocumentStoragePath = document.BlobPath,
                DocumentHash = document.DocumentHash,
                Status = document.Status,
                SubmittedBy = document.PatientMRN ?? "unknown",
                SubmittedAt = document.SubmittedAt,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            _dbContext.Referrals.Add(referral);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Referral metadata stored in SQL Server: {ReferralId}", document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing referral metadata");
            throw;
        }
    }

    private static string CalculateSHA256Hash(byte[] data)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedData = sha256.ComputeHash(data);
            return BitConverter.ToString(hashedData).Replace("-", "").ToLowerInvariant();
        }
    }

    private static string GetFileExtension(string documentFormat)
    {
        return documentFormat.ToLowerInvariant() switch
        {
            "pdf" => "pdf",
            "txt" => "txt",
            "text" => "txt",
            "png" => "png",
            "jpg" => "jpg",
            "jpeg" => "jpg",
            _ => "bin"
        };
    }
}
