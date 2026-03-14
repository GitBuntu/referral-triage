using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ReferralTriageApp.Models;
using ReferralTriageApp.Services;

namespace ReferralTriageApp.Functions;

public class ReferralIntakeFunction
{
    private readonly IReferralIntakeService _referralIntakeService;
    private readonly ILogger<ReferralIntakeFunction> _logger;

    public ReferralIntakeFunction(
        IReferralIntakeService referralIntakeService,
        ILogger<ReferralIntakeFunction> logger)
    {
        _referralIntakeService = referralIntakeService;
        _logger = logger;
    }

    [Function("ReferralIntake")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "referrals/intake")] HttpRequestData req)
    {
        _logger.LogInformation("ReferralIntake function triggered");

        try
        {
            // Read request body
            var body = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body cannot be empty");
            }

            // Parse JSON
            var request = JsonSerializer.Deserialize<ReferralIntakeRequest>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            // Process referral
            var response = await _referralIntakeService.ProcessReferralAsync(request);

            // Return success response
            var successResponse = req.CreateResponse(HttpStatusCode.Accepted);
            await successResponse.WriteAsJsonAsync(response);
            successResponse.Headers.Add("Content-Type", "application/json");
            return successResponse;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in ReferralIntake");
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing referral intake");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError,
                "An error occurred processing the referral");
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        var errorResponse = new ErrorResponse { Message = message };
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteAsJsonAsync(errorResponse);
        return response;
    }
}
