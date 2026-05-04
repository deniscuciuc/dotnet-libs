namespace DenisCuciuc.Platform.Identity.Settings;

public sealed class ServiceOptions
{
    public const string DefaultSectionPath = "Identity:Service";

    public ServiceVersion Version { get; set; } = null!;

    public string VersionAlt { get; set; } = null!;

    public bool IsDevEnvironment { get; set; }

    public string EnvironmentName { get; set; } = null!;

    public string EnvironmentNewLine => Environment.NewLine;
}

public sealed class ServiceVersion
{
    public ServiceVersion()
    {
    }

    public ServiceVersion(int major, int minor, int build)
    {
        Major = major;
        Minor = minor;
        Build = build;
    }

    public int Major { get; set; }

    public int Minor { get; set; }

    public int Build { get; set; }
}
