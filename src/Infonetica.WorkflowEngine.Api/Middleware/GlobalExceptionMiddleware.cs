using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Infonetica.WorkflowEngine.Application.Services;

namespace Infonetica.WorkflowEngine.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions.
/// I make sure users get helpful error messages instead of scary technical details.
/// I also log everything so developers can debug issues later.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles different types of exceptions and returns appropriate responses.
    /// I categorize errors so users know what went wrong and how to fix it.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        var errorResponse = CreateErrorResponse(exception, correlationId);

        // Log with different levels based on exception type
        switch (exception)
        {
            case ValidationException:
                _logger.LogWarning("Validation error occurred. CorrelationId: {CorrelationId}, Path: {Path}, Error: {Error}",
                    correlationId, context.Request.Path, exception.Message);
                break;
            case ConcurrencyException:
                _logger.LogInformation("Concurrency conflict occurred. CorrelationId: {CorrelationId}, Path: {Path}, Error: {Error}",
                    correlationId, context.Request.Path, exception.Message);
                break;
            case KeyNotFoundException:
                _logger.LogWarning("Resource not found. CorrelationId: {CorrelationId}, Path: {Path}, Error: {Error}",
                    correlationId, context.Request.Path, exception.Message);
                break;
            default:
                _logger.LogError(exception, "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                    correlationId, context.Request.Path, context.Request.Method);
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetStatusCode(exception);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }

    /// <summary>
    /// Creates a user-friendly error response based on the exception type.
    /// I make sure users get helpful information without exposing sensitive details.
    /// </summary>
    private static ErrorResponse CreateErrorResponse(Exception exception, string correlationId)
    {
        return exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                Type = "ValidationError",
                Title = "Validation Failed",
                Detail = validationEx.Message,
                Status = 400,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            },
            ConcurrencyException concurrencyEx => new ErrorResponse
            {
                Type = "ConcurrencyError",
                Title = "Conflict Detected",
                Detail = concurrencyEx.Message,
                Status = 409,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            },
            KeyNotFoundException notFoundEx => new ErrorResponse
            {
                Type = "NotFound",
                Title = "Resource Not Found",
                Detail = notFoundEx.Message,
                Status = 404,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            },
            TimeoutException timeoutEx => new ErrorResponse
            {
                Type = "Timeout",
                Title = "Operation Timeout",
                Detail = "The operation took too long to complete. Please try again.",
                Status = 408,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            },
            _ => new ErrorResponse
            {
                Type = "InternalServerError",
                Title = "An unexpected error occurred",
                Detail = "Something went wrong on our end. Please try again later or contact support if the problem persists.",
                Status = 500,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            }
        };
    }

    /// <summary>
    /// Maps exception types to HTTP status codes.
    /// I follow standard HTTP conventions so clients know how to handle responses.
    /// </summary>
    private static int GetStatusCode(Exception exception) => exception switch
    {
        ValidationException => 400,        // Bad Request
        ConcurrencyException => 409,       // Conflict
        KeyNotFoundException => 404,       // Not Found
        TimeoutException => 408,           // Request Timeout
        UnauthorizedAccessException => 401, // Unauthorized
        _ => 500                           // Internal Server Error
    };
}

/// <summary>
/// Standard error response format for all API errors.
/// I provide consistent error information that's easy for clients to parse.
/// </summary>
public record ErrorResponse
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public int Status { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public Dictionary<string, object> Extensions { get; init; } = new();
}
