using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ruru_api.common;

namespace ruru_api.Tests.Unit;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler = new(NullLogger<GlobalExceptionHandler>.Instance);

    public static TheoryData<Exception, int> Cases => new()
    {
        { new UnauthorizedAccessException(), StatusCodes.Status401Unauthorized },
        { new KeyNotFoundException(), StatusCodes.Status404NotFound },
        { new ArgumentException("bad"), StatusCodes.Status400BadRequest },
        { new ArgumentNullException("value"), StatusCodes.Status400BadRequest },
        { new DbUpdateException("conflict"), StatusCodes.Status409Conflict },
        { new InvalidOperationException(), StatusCodes.Status409Conflict },
        { new Exception("boom"), StatusCodes.Status500InternalServerError },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task MapsExceptionToStatusCodeAndErrorResponse(Exception exception, int expectedStatus)
    {
        var context = NewContext();

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        var body = ReadBody(context);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));
        Assert.Equal(1, body.GetProperty("errors").GetArrayLength());
    }

    [Fact]
    public async Task DoesNotSendExceptionDetailsToTheClient()
    {
        var context = NewContext();

        await _handler.TryHandleAsync(context, new Exception("Npgsql: password authentication failed for user admin"), CancellationToken.None);

        context.Response.Body.Position = 0;
        var raw = new StreamReader(context.Response.Body).ReadToEnd();
        Assert.DoesNotContain("password", raw);
        Assert.DoesNotContain("admin", raw);
    }

    [Fact]
    public async Task WhenClientCancelled_WritesNothing()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var context = NewContext();
        context.RequestAborted = cancelled.Token;

        var handled = await _handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(0, context.Response.Body.Length);
    }

    private static DefaultHttpContext NewContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static JsonElement ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body).RootElement;
    }
}
