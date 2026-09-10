namespace CoreLibs.LiveConfig.GSheet;

public class GSheetOptions
{
    public const string SectionPath = "LiveConfig:GSheet";

    /// <summary>
    ///     Google Sheets API application name.
    /// </summary>
    public string ApplicationName { get; set; } = "CoreLibs.LiveConfig.GSheet";

    /// <summary>
    ///     Spreadsheet ID of the Google Sheet to import data from.
    /// </summary>
    public string SpreadsheetId { get; set; } = string.Empty;

    /// <summary>
    ///     Credentials configuration for accessing Google Sheets API.
    /// </summary>
    public GSheetCredentials Credentials { get; set; } = new();

    /// <summary>
    ///     CSV options for parsing Google Sheets data.
    /// </summary>
    public GSheetCsvOptions Csv { get; set; } = new();

    /// <summary>
    ///     When true, the application will fail if any validation errors occur during import.
    ///     When false (default), validation errors are logged as warnings and invalid rows are skipped.
    /// </summary>
    public bool FailOnValidationErrors { get; set; }

    /// <summary>
    ///     When true, the application will fail if any errors occur during data fetching or import.
    ///     When false (default), errors are logged and the import continues.
    /// </summary>
    public bool FailOnImportErrors { get; set; }

    /// <summary>
    ///     Controls how validation errors are handled in pipeline importers.
    ///     Default is <see cref="GSheetImportMode.SkipInvalid"/> for backward compatibility.
    /// </summary>
    public GSheetImportMode ImportMode { get; set; } = GSheetImportMode.SkipInvalid;

    /// <summary>
    ///     When true, runs the full pipeline but does not commit domain data or call ProcessAsync.
    ///     Useful for validation previews and CI checks.
    /// </summary>
    public bool DryRun { get; set; }
}
