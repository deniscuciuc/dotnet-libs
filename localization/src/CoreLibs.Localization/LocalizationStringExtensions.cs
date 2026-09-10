namespace CoreLibs.Localization;

public static class LocalizationStringExtensions
{
    public static bool IsValidLanguageCode(this string code)
    {
        try
        {
            _ = new LanguageCode(code);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool IsValidLocalizationKey(this string key)
    {
        try
        {
            _ = new LocalizationKey(key);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
