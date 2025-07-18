using System.Diagnostics;

namespace Infonetica.WorkflowEngine.Api.Middleware;

/// <summary>
/// Middleware that tracks how long requests take to process.
/// I help identify slow operations and potential performance bottlenecks.
/// If a request takes too long, I log a warning so developers can investigate.
/// </summary>
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            
            // Log slow requests for performance monitoring
            if (elapsedMs > 1000) // Requests taking more than 1 second
            {
                _logger.LogWarning("Slow request detected: {Method} {Path} took {ElapsedMs}ms (CorrelationId: {CorrelationId})",
                    context.Request.Method, 
                    context.Request.Path, 
                    elapsedMs,
                    context.TraceIdentifier);
            }
            else
            {
                _logger.LogDebug("Request completed: {Method} {Path} took {ElapsedMs}ms",
                    context.Request.Method, 
                    context.Request.Path, 
                    elapsedMs);
            }
            
            // Add timing information to response headers for clients
            context.Response.Headers["X-Response-Time-ms"] = elapsedMs.ToString();
        }
    }
}
