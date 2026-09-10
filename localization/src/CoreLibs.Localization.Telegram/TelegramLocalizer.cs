namespace CoreLibs.Localization.Telegram;

/// <summary>
/// Telegram-aware localizer that auto-resolves culture from user context.
/// </summary>
public sealed class TelegramLocalizer
{
    private readonly ILocalizer _localizer;
    private readonly ITelegramUserContext _context;
    private readonly LocalizationOptions _options;

    public TelegramLocalizer(
        ILocalizer localizer,
        ITelegramUserContext context,
        Microsoft.Extensions.Options.IOptions<LocalizationOptions> options)
    {
        _localizer = localizer;
        _context = context;
        _options = options.Value;
    }

    /// <summary>
    /// Returns a user-scoped localizer. Usage: (await localizer.ForUserAsync(userId)).Get("key").
    /// </summary>
    public async ValueTask<UserLocalizer> ForUserAsync(long userId)
    {
        var culture = await _context.GetUserCultureAsync(userId) ?? _options.DefaultCulture;
        return new UserLocalizer(_localizer, culture);
    }
}

/// <summary>
/// User-scoped localizer — resolves strings without specifying culture each time.
/// </summary>
public sealed class UserLocalizer(ILocalizer localizer, string culture)
{
    public string Get(string key)
    {
        return localizer.Get(key, culture);
    }

    public string Get(string key, LocalizationArgs args)
    {
        return localizer.Get(key, culture, args);
    }

    public string GetPlural(string key, long count, LocalizationArgs args = default)
    {
        return localizer.GetPlural(key, culture, count, args);
    }
}
