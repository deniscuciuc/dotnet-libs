using CoreLibs.Localization;

namespace CoreLibs.Localization.Examples.Worker.Importers;

/// <summary>
/// Bulk-imports a <see cref="LocalizationSnapshot"/> into the persistent <see cref="ILocalizationStore"/>.
/// Useful for seeding MongoDB with locale data produced by <see cref="CoreLibs.Localization.LiveConfig.Json.LocalizationJsonLoader"/>
/// or <see cref="CoreLibs.Localization.LiveConfig.GSheet.LocalizationGSheetConverter"/>.
/// </summary>
public sealed class SnapshotStoreImporter(
    ILocalizationStore store,
    ILogger<SnapshotStoreImporter> logger)
{
    public async Task ImportAsync(LocalizationSnapshot snapshot, CancellationToken ct = default)
    {
        await store.EnsureCreatedAsync(ct);

        var count = 0;
        foreach (var (key, value) in snapshot.Entries)
        {
            await store.UpsertAsync(new LocalizationEntry
            {
                Culture = snapshot.Culture,
                Key = key,
                Value = value
            }, ct);
            count++;
        }

        logger.LogInformation(
            "Imported {Count} entries for culture '{Culture}' into the persistent store.",
            count, snapshot.Culture);
    }
}
