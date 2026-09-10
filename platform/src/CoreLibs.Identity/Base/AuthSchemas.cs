using CoreLibs.Identity.Validators.DebugValidator;
using CoreLibs.Identity.Validators.Internal;
using CoreLibs.Identity.Validators.Production;
using CoreLibs.Identity.Validators.Telegram;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CoreLibs.Identity.Base;

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
