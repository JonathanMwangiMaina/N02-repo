namespace BlackoutClause.Server.Endpoints;

using BlackoutClause.Server.Infrastructure.Clerk;
using BlackoutClause.Server.Infrastructure.Data;
using BlackoutClause.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Clerk webhook endpoint for receiving authentication and subscription events.
/// </summary>
public static class ClerkWebhookEndpoints
{
    /// <summary>
    /// Maps Clerk webhook endpoint to the route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapClerkWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(ApiConstants.Endpoints.ClerkWebhook, HandleClerkWebhookAsync)
           .WithName("ClerkWebhook")
           .DisableAntiforgery()
           .AllowAnonymous()
           .Produces(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Handles incoming Clerk webhook events with signature verification.
    /// </summary>
    /// <param name="request">The HTTP request containing the webhook payload.</param>
    /// <param name="webhookHandler">Clerk webhook handler service.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>OK if processed successfully, BadRequest otherwise.</returns>
    private static async Task<IResult> HandleClerkWebhookAsync(
        HttpRequest request,
        IClerkWebhookHandler webhookHandler,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var body = await new StreamReader(request.Body).ReadToEndAsync(ct);
        var signature = request.Headers["clerk-signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("Missing clerk-signature header");
            return Results.BadRequest();
        }

        try
        {
            await webhookHandler.HandleAsync(body, signature, ct);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process Clerk webhook");
            return Results.BadRequest();
        }
    }
}
