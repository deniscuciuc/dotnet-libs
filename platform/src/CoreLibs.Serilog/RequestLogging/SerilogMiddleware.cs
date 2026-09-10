using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CoreLibs.Serilog.RequestLogging;

public class SerilogMiddleware(RequestDelegate next, RequestBodyOptions bodyOptions)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var diagnosticContext = ctx.RequestServices.GetService<IDiagnosticContext>();

        if (bodyOptions.LogRequestBody)
        {
            try
            {
                if (ctx.Request.ContentLength > 0 && (ctx.Request.ContentType?.Contains("application/json") ?? false))
                {
                    ctx.Request.EnableBuffering();
                    using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, false, 1024, true);
                    var body = await reader.ReadToEndAsync();
                    ctx.Request.Body.Position = 0;

                    if (body.Length > bodyOptions.MaxRequestBodyLength)
                        body = body[..bodyOptions.MaxRequestBodyLength] + "...";

                    diagnosticContext?.Set("RequestBody", body);
                }
            }
            catch
            {
                // ignore body read errors
            }
        }

        if (!bodyOptions.LogResponseBody)
        {
            await next(ctx);
            return;
        }

        var originalBody = ctx.Response.Body;
        await using var mem = new MemoryStream();
        ctx.Response.Body = mem;

        try
        {
            await next(ctx);
        }
        finally
        {
            mem.Position = 0;
            try
            {
                using var respReader = new StreamReader(mem, Encoding.UTF8, false, leaveOpen: true);
                var responseBody = await respReader.ReadToEndAsync();
                if (responseBody.Length > bodyOptions.MaxResponseBodyLength)
                    responseBody = responseBody[..bodyOptions.MaxResponseBodyLength] + "...";
                diagnosticContext?.Set("ResponseBody", responseBody);
            }
            catch
            {
                // ignore body read errors
            }

            mem.Position = 0;
            await mem.CopyToAsync(originalBody);
            ctx.Response.Body = originalBody;
        }
    }
}
