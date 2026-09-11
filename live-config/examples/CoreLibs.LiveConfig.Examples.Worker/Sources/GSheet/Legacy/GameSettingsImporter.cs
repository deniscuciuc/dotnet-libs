using CoreLibs.LiveConfig.Examples.Worker.Domain;
using CoreLibs.LiveConfig.GSheet;
using CsvHelper.Configuration;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.Legacy;

/// <summary>
/// Example GSheet row for game settings.
/// Columns: Key | Value
/// </summary>
public sealed class GameSettingsRow
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Example GSheet importer for game settings.
/// Reads key-value pairs from a 'GameSettings' sheet and maps to a typed config.
/// </summary>
[GSheetImporter("GameSettings", "A1:B")]
public class GameSettingsImporter : IGSheetImporter<GameSettingsRow, GameSettingsConfig>
{
    public ClassMap<GameSettingsRow> CreateMapper()
    {
        return new GameSettingsRowMap();
    }

    public GSheetValidationResult ValidateRows(IReadOnlyList<GameSettingsRow> rows)
    {
        var errors = new List<GSheetValidationError>();

        for (var i = 0; i < rows.Count; i++)
            if (string.IsNullOrWhiteSpace(rows[i].Key))
                errors.Add(new GSheetValidationError(i, nameof(GameSettingsRow.Key), "Key is required"));

        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }

    public IEnumerable<GameSettingsConfig> MapToDomain(
        IReadOnlyList<GameSettingsRow> rows,
        GSheetImportContext context)
    {
        var dict = rows.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);

        yield return new GameSettingsConfig(
            int.TryParse(dict.GetValueOrDefault("MaxPlayers"), out var mp) ? mp : 100,
            int.TryParse(dict.GetValueOrDefault("RoundDurationSeconds"), out var rd) ? rd : 60,
            decimal.TryParse(dict.GetValueOrDefault("MinBetAmount"), out var minBet) ? minBet : 1m,
            decimal.TryParse(dict.GetValueOrDefault("MaxBetAmount"), out var maxBet) ? maxBet : 1000m,
            bool.TryParse(dict.GetValueOrDefault("MaintenanceMode"), out var mm) && mm);
    }

    public Task<bool> ImportAsync(IEnumerable<GameSettingsConfig> domains, GSheetImportContext context)
    {
        // In a real application you would persist to your store here.
        // The framework handles domain caching internally.
        return Task.FromResult(true);
    }
}

public sealed class GameSettingsRowMap : ClassMap<GameSettingsRow>
{
    public GameSettingsRowMap()
    {
        Map(m => m.Key).Name("Key");
        Map(m => m.Value).Name("Value");
    }
}
