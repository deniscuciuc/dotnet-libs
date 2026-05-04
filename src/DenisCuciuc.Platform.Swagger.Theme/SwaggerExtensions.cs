using Microsoft.AspNetCore.Builder;

namespace DenisCuciuc.Platform.Swagger.Theme;

public static class SwaggerExtensions
{
    public static IApplicationBuilder UsePlatformSwagger(
        this IApplicationBuilder app,
        Action<Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIOptions>? configureUI = null)
    {
        app.UseSwagger();
        app.UseSwaggerUI(AspNetCore.Swagger.Themes.Theme.Dark, c =>
        {
            c.EnableAllAdvancedOptions();
            configureUI?.Invoke(c);
        });
        return app;
    }

}
