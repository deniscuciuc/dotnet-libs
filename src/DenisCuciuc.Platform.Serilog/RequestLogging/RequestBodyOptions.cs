namespace DenisCuciuc.Platform.Serilog.RequestLogging;

public sealed class RequestBodyOptions
{
    /// <summary>Capture and log the JSON request body. Disabled by default â€” opt in explicitly.</summary>
    public bool LogRequestBody { get; set; }

    /// <summary>Capture and log the response body. Disabled by default â€” opt in explicitly.</summary>
    public bool LogResponseBody { get; set; }

    public int MaxRequestBodyLength { get; set; } = 2048;
    public int MaxResponseBodyLength { get; set; } = 4096;
}
