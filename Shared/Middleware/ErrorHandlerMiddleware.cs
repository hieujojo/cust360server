using System.Text.Json;
using CRM.Api.Shared.Exceptions;

namespace CRM.Api.Shared.Middleware;

/// <summary>Global exception handler. Trả về ProblemDetails chuẩn RFC 7807.</summary>
public sealed class ErrorHandlerMiddleware
{
    private readonly RequestDelegate              _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
    {
        _next   = next;
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail, errors) = exception switch
        {
            NotFoundException e      => (404, "Not Found",            e.Message, (object?)null),
            ForbiddenException e     => (403, "Forbidden",            e.Message, (object?)null),
            UnauthorizedException e  => (401, "Unauthorized",         e.Message, (object?)null),
            ConflictException e      => (409, "Conflict",             e.Message, (object?)null),
            ValidationException e    => (400, "Validation Error",     e.Message, (object?)e.Errors),
            _                        => (500, "Internal Server Error","Đã xảy ra lỗi hệ thống.", (object?)null)
        };

        if (statusCode == 500)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        context.Response.StatusCode = statusCode;

        var problem = new
        {
            type    = $"https://tools.ietf.org/html/rfc7231#section-6.{statusCode / 100}.{statusCode % 100}",
            title,
            status  = statusCode,
            detail,
            errors,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
