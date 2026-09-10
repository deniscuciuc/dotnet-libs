namespace CoreLibs.LiveConfig.GSheet;

public class GSheetCredentials
{
    public GSheetCredentialsMethod Method { get; set; } = GSheetCredentialsMethod.CredentialsBase64Env;

    /// <summary>
    ///     Value meaning depends on Method:
    ///     - Base64JsonFromConfiguration: Base64-encoded service account JSON content
    ///     - CredentialsBase64Env: Environment variable name holding Base64-encoded JSON
    ///     - Path: Full path to credentials JSON file
    /// </summary>
    public string? Value { get; set; }
}
