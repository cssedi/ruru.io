using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace ruru_api.common;

/// <summary>
/// Turns any exception a controller doesn't handle into an <see cref="ErrorResponse"/> with a fitting
/// status code, so every endpoint fails the same way. Registered in Program.cs.
/// Messages are fixed text: exception details are logged, never sent to the client.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The client closed the connection, so there's no one to send a response to.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (statusCode, message) = Map(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode} on {Method} {Path}", statusCode, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse(message), cancellationToken);

        return true;
    }

    private static (int StatusCode, string Message) Map(Exception exception) => exception switch
    {
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Sign in to continue."),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "We couldn't find what you were looking for."),
        ArgumentException => (StatusCodes.Status400BadRequest, "The request isn't valid. Check the details and try again."),
        DbUpdateException => (StatusCodes.Status409Conflict, "That change clashes with existing data. Refresh and try again."),
        InvalidOperationException => (StatusCodes.Status409Conflict, "That can't be done right now. Refresh and try again."),
        _ => (StatusCodes.Status500InternalServerError, "Something went wrong on our side. Try again in a moment."),
    };
}
