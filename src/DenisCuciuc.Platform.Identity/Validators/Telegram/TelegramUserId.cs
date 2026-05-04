namespace DenisCuciuc.Platform.Identity.Validators.Telegram;

public static class TelegramUserId
{
    public static string GeExternalUserIdByTelegramUserId(string telegramUserId)
    {
        return $"telegram|{telegramUserId}";
    }
}
