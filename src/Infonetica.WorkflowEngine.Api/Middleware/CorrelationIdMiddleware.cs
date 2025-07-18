namespace Infonetica.WorkflowEngine.Api.Middleware;

/// <summary>
/// Middleware that adds correlation IDs to requests for tracking.
/// I make sure every request has a unique ID so we can trace problems across logs.
/// This is super helpful when debugging issues in production.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get correlation ID from request header or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() 
                           ?? Guid.NewGuid().ToString();

        // Set the correlation ID for this request
        context.TraceIdentifier = correlationId;
        
        // Add correlation ID to response headers so clients can track their requests
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        await _next(context);
    }
}
