namespace CoreLibs.Localization.Store.Postgres;

public sealed class PostgresLocalizationStoreOptions
{
    public const string SectionPath = "Localization:Store:Postgres";

    public string Schema { get; set; } = "public";
    public string TableName { get; set; } = "localization_entries";
}
