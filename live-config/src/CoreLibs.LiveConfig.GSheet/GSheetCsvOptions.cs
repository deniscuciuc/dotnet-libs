using System.Globalization;

namespace CoreLibs.LiveConfig.GSheet;

public class GSheetCsvOptions
{
    /// <summary>
    ///     CSV delimiter used when synthesizing CSV from Google Sheets values.
    /// </summary>
    public string Delimiter { get; set; } = "#";

    /// <summary>
    ///     Culture name (e.g., "en-US"). If null or empty, InvariantCulture is used.
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    ///     When true, CsvHelper will not throw for header validation issues.
    /// </summary>
    public bool IgnoreHeaderValidated { get; set; } = true;

    /// <summary>
    ///     When true, CsvHelper will not throw when a field is missing.
    /// </summary>
    public bool IgnoreMissingField { get; set; } = true;

    public CultureInfo ResolveCulture()
    {
        return string.IsNullOrWhiteSpace(Culture) ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(Culture);
    }
}
