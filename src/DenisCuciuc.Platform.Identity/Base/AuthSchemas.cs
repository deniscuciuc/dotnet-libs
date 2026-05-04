using DenisCuciuc.Platform.Identity.Validators.DebugValidator;
using DenisCuciuc.Platform.Identity.Validators.Internal;
using DenisCuciuc.Platform.Identity.Validators.Production;
using DenisCuciuc.Platform.Identity.Validators.Telegram;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace DenisCuciuc.Platform.Identity.Base;

public static class AuthSchemas
{
    public const string DefaultAuth =
        ServiceDebugAuthenticationOptions.SchemeName + ", " +
        ServiceProdAuthenticationOptions.SchemeName + ", " +
        TelegramAuthenticationOptions.SchemeName + ", " +
        JwtBearerDefaults.AuthenticationScheme;

    public const string DefaultInternalAuth =
        ServiceDebugAuthenticationOptions.SchemeName + ", " +
        ServiceInternalAuthenticationOptions.SchemeName + ", " +
        ServiceProdAuthenticationOptions.SchemeName + ", " +
        TelegramAuthenticationOptions.SchemeName + ", " +
        JwtBearerDefaults.AuthenticationScheme;
}
