using HakwadagAssassinGame.Application.Exceptions;
using System.Text.Json;

namespace HakwadagAssassinGame.Web.Middleware;

/// <summary>Converts application failures into consistent HTTP responses.</summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    /// <summary>Initializes the exception handling middleware.</summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    /// <summary>Executes the request and maps known application exceptions.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            logger.LogError(exception, "An exception occurred after the response started.");
            throw exception;
        }

        var statusCode = exception switch
        {
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            GameNotFoundException or PlayerNotFoundException or AssignmentNotFoundException or TagSubmissionNotFoundException
                => StatusCodes.Status404NotFound,
            InvalidGameStateException or SafeTimeBlockViolationException or PendingTagExistsException
                => StatusCodes.Status400BadRequest,
            ArgumentException or FormatException or JsonException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "An unhandled exception occurred while processing the request.");
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            new { error = statusCode == StatusCodes.Status500InternalServerError ? "An internal server error occurred." : exception.Message },
            context.RequestAborted);
    }
}
