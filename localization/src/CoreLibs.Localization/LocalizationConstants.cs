using System.Security.Cryptography;
using System.Text;

namespace CoreLibs.Localization;

/// <summary>
/// Shared constants and helpers for the CoreLibs Localization framework.
/// </summary>
public static class LocalizationConstants
{
    public const string ConfigTypePrefix = "localization";
    public const string JsonSourceName = "localization-json";
    public const string GSheetSourceName = "localization-gsheet";
    public const string GSheetTabName = "localization";
    public const string DefaultLocalesDirectory = "locales";

    public static string ConfigType(string culture)
    {
        return $"{ConfigTypePrefix}:{culture}";
    }

    public static string ComputeHash(string json)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(bytes);
    }
}
