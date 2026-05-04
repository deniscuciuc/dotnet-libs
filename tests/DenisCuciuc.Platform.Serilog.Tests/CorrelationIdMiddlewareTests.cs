using DenisCuciuc.Platform.Serilog.CorrelationId;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace DenisCuciuc.Platform.Serilog.Tests;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_GeneratesNewId_WhenHeaderAbsent()
    {
        var options = new CorrelationIdOptions { HeaderName = "X-Correlation-ID", IncludeInResponse = false };
        var ctx = new DefaultHttpContext();
        var capturedId = string.Empty;

        Task Next(HttpContext c)
        {
            capturedId = c.Request.Headers["X-Correlation-ID"].ToString();
            return Task.CompletedTask;
        }

        var middleware = new CorrelationIdMiddleware(Next, options);
        await middleware.InvokeAsync(ctx);

        Assert.False(string.IsNullOrWhiteSpace(capturedId));
        Assert.True(Guid.TryParse(capturedId, out _));
    }

    [Fact]
    public async Task InvokeAsync_PreservesExistingId_WhenHeaderPresent()
    {
        const string existingId = "my-correlation-id";
        var options = new CorrelationIdOptions { HeaderName = "X-Correlation-ID", IncludeInResponse = false };

        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Correlation-ID"] = existingId;

        var capturedId = string.Empty;

        Task Next(HttpContext c)
        {
            capturedId = c.Request.Headers["X-Correlation-ID"].ToString();
            return Task.CompletedTask;
        }

        var middleware = new CorrelationIdMiddleware(Next, options);
        await middleware.InvokeAsync(ctx);

        Assert.Equal(existingId, capturedId);
    }

    [Fact]
    public async Task InvokeAsync_EchoesIdInResponse_WhenIncludeInResponseIsTrue()
    {
        const string existingId = "echo-me";
        var options = new CorrelationIdOptions { HeaderName = "X-Correlation-ID", IncludeInResponse = true };

        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Correlation-ID"] = existingId;

        var capturingFeature = new CapturingResponseFeature();
        ctx.Features.Set<IHttpResponseFeature>(capturingFeature);

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, options);
        await middleware.InvokeAsync(ctx);

        await capturingFeature.FireOnStartingAsync();

        Assert.Equal(existingId, ctx.Response.Headers["X-Correlation-ID"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_DoesNotEchoId_WhenIncludeInResponseIsFalse()
    {
        var options = new CorrelationIdOptions { HeaderName = "X-Correlation-ID", IncludeInResponse = false };
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Correlation-ID"] = "do-not-echo";

        await new CorrelationIdMiddleware(_ => Task.CompletedTask, options).InvokeAsync(ctx);

        Assert.False(ctx.Response.Headers.ContainsKey("X-Correlation-ID"));
    }

    [Fact]
    public async Task InvokeAsync_UsesCustomHeaderName()
    {
        var options = new CorrelationIdOptions { HeaderName = "X-Request-ID", IncludeInResponse = false };
        var ctx = new DefaultHttpContext();

        var capturedId = string.Empty;

        await new CorrelationIdMiddleware(c =>
        {
            capturedId = c.Request.Headers["X-Request-ID"].ToString();
            return Task.CompletedTask;
        }, options).InvokeAsync(ctx);

        Assert.False(string.IsNullOrWhiteSpace(capturedId));
    }

    // Minimal IHttpResponseFeature that captures and can fire OnStarting callbacks
    private sealed class CapturingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _callbacks = new();

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => false;

        public void OnStarting(Func<object, Task> callback, object state) =>
            _callbacks.Add((callback, state));

        public void OnCompleted(Func<object, Task> callback, object state) { }

        public async Task FireOnStartingAsync()
        {
            foreach (var (callback, state) in _callbacks)
                await callback(state);
        }
    }
}
