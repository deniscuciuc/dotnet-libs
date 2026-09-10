using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreLibs.Localization.Telegram;

public static class TelegramLocalizationExtensions
{
    /// <summary>
    /// Registers <see cref="TelegramLocalizer"/> and allows custom <see cref="ITelegramUserContext"/>.
    /// </summary>
    public static IServiceCollection AddCoreLocalizationTelegram<TUserContext>(
        this IServiceCollection services)
        where TUserContext : class, ITelegramUserContext
    {
        services.TryAddSingleton<ITelegramUserContext, TUserContext>();
        services.TryAddSingleton<TelegramLocalizer>();
        return services;
    }
}
