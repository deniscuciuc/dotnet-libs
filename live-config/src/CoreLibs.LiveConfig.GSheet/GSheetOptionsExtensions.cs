using Microsoft.Extensions.Configuration;

namespace CoreLibs.LiveConfig.GSheet;

public static class GSheetOptionsExtensions
{
    public static GSheetOptions GetGSheetOptions(this IConfiguration configuration,
        string section = GSheetOptions.SectionPath)
    {
        return configuration.GetSection(section).Get<GSheetOptions>() ??
               throw new InvalidOperationException($"Configuration section '{section}' is missing or invalid.");
    }
}
