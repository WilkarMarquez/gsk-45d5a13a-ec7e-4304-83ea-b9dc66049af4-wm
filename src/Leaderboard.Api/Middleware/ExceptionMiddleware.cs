using System.Text.Json;
using FluentValidation;

namespace Leaderboard.Api.Middleware;
public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}",
                context.TraceIdentifier);

            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,

            ValidationException => StatusCodes.Status400BadRequest,

            ArgumentException => StatusCodes.Status400BadRequest,

            InvalidOperationException => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new
        {
            title = GetTitle(statusCode),
            status = statusCode,
            detail = statusCode == StatusCodes.Status500InternalServerError
                                  ? "An unexpected error occurred."
                                  : exception.Message,
        };

        var json = JsonSerializer.Serialize(problem);

        await context.Response.WriteAsync(json);
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",

            StatusCodes.Status404NotFound => "Resource Not Found",

            StatusCodes.Status409Conflict => "Conflict",

            _ => "Internal Server Error"
        };
    }
}