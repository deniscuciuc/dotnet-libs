using Microsoft.AspNetCore.Http;

namespace DenisCuciuc.Platform.Serilog.CorrelationId;

internal sealed class CorrelationIdMiddleware(RequestDelegate next, CorrelationIdOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[options.HeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
            // Set on the request so Serilog.Enrichers.CorrelationId can read it
            // via IHttpContextAccessor.
            context.Request.Headers[options.HeaderName] = id;
        }

        if (options.IncludeInResponse)
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[options.HeaderName] = id;
                return Task.CompletedTask;
            });

        await next(context);
    }
}
