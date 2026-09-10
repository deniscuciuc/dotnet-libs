using CoreLibs.Identity.Identity;
using CoreLibs.Identity.Settings;
using CoreLibs.Identity.Token;
using CoreLibs.Identity.Validators.DebugValidator;
using CoreLibs.Identity.Validators.Internal;
using CoreLibs.Identity.Validators.Production;
using CoreLibs.Identity.Validators.Telegram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Identity;

public static class IdentityExtensions
{
    /// <summary>
    /// Registers all token validators in priority order.
    /// </summary>
    public static IServiceCollection AddCoreTokenValidators(this IServiceCollection services)
    {
        services
            .AddSingleton<IIdentityDecoder, IdentityDecoder>()
            .AddSingleton<IOrderedTokenValidator, AnonymousJwtTokenValidator>()
            .AddSingleton<IOrderedTokenValidator, OrganizationApiKeyJwtTokenValidator>()
            .AddSingleton<IOrderedTokenValidator, TelegramJWTValidator>()
            .AddSingleton<IOrderedTokenValidator, OpenCompositeTokenValidator>()
            .AddSingleton<IOrderedTokenValidator, InternalSecretValidator>();

        services.AddSingleton<IReadOnlyList<ITokenValidator>>(sp =>
        {
            return sp.GetRequiredService<IEnumerable<IOrderedTokenValidator>>()
                .OrderBy(v => v.Order)
                .Cast<ITokenValidator>()
                .ToList()
                .AsReadOnly();
        });

        return services;
    }

    /// <summary>
    /// Registers identity options, authentication schemes and authorization.
    /// Configuration sections are resolved from <c>Identity:*</c> by default.
    /// </summary>
    public static IServiceCollection AddCoreIdentity(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ServiceOptions>()
            .Bind(configuration.GetSection(ServiceOptions.DefaultSectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<Auth0Options>()
            .Bind(configuration.GetSection(Auth0Options.DefaultSectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<AnonymousAuthOptions>()
            .Bind(configuration.GetSection(AnonymousAuthOptions.DefaultSectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<TelegramAuthOptions>()
            .Bind(configuration.GetSection(TelegramAuthOptions.DefaultSectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<ApiTokenAuthOptions>()
            .Bind(configuration.GetSection(ApiTokenAuthOptions.DefaultSectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var auth0Section = configuration.GetSection(Auth0Options.DefaultSectionPath);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
            }).AddJwtBearer(options =>
            {
                options.Authority = $"https://{auth0Section["Domain"]}/";
                options.Audience = auth0Section["ApiAudience"];
            }).AddScheme<ServiceDebugAuthenticationOptions, ServiceDebugAuthenticationHandler>(
                ServiceDebugAuthenticationOptions.SchemeName,
                options =>
                {
                    options.Secret = configuration["ServiceAuth:DebugSecret"] ??
                                     throw new InvalidOperationException("Debug secret is required");
                })
            .AddScheme<ServiceInternalAuthenticationOptions, ServiceInternalAuthenticationHandler>(
                ServiceInternalAuthenticationOptions.SchemeName,
                options =>
                {
                    options.Secret = configuration["ServiceAuth:InternalSecret"] ??
                                     throw new InvalidOperationException("Internal secret is required");
                })
            .AddScheme<TelegramAuthenticationOptions, TelegramAuthenticationHandler>(
                TelegramAuthenticationOptions.SchemeName, _ => { })
            .AddScheme<ServiceProdAuthenticationOptions, ServiceProdAuthenticationHandler>(
                ServiceProdAuthenticationOptions.SchemeName,
                options =>
                {
                    options.Secret = configuration["ServiceAuth:JwtSecret"] ??
                                     throw new InvalidOperationException("Jwt secret is required");
                });

        services.AddAuthorization();

        return services;
    }

}
