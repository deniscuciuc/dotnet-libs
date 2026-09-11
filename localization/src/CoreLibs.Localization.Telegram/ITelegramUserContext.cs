namespace CoreLibs.Localization.Telegram;

/// <summary>
/// Provides the language code for a Telegram user.
/// </summary>
public interface ITelegramUserContext
{
    ValueTask<string?> GetUserCultureAsync(long userId);
}
