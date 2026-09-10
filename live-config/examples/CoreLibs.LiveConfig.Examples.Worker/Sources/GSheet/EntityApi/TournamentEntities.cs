using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.EntityApi;

// ═══════════════════════════════════════════════════════════════
// Tier 2 — Tournament Reward (attribute + config with graph validation)
// Compare with: Sources/GSheet/Pipeline/TournamentImporter.cs (TournamentRewardImporter + 2 validators)
// ═══════════════════════════════════════════════════════════════

[GSheetEntity("TournamentRewards", "A1:F")]
public record TournamentRewardEntity(
    [property: GSheetColumn("TournamentId")]
    [property: GSheetRequired]
    string TournamentId,
    [property: GSheetColumn("PlaceFrom")]
    [property: GSheetRequired]
    [property: GSheetRange(1, 10000)]
    int PlaceFrom,
    [property: GSheetColumn("PlaceTo")]
    [property: GSheetRequired]
    [property: GSheetRange(1, 10000)]
    int PlaceTo,
    [property: GSheetColumn("RewardCurrency")]
    [property: GSheetRequired]
    [property: GSheetRef(typeof(CurrencyEntity), nameof(CurrencyEntity.Code))]
    string RewardCurrency,
    [property: GSheetColumn("Amount")]
    [property: GSheetRequired]
    [property: GSheetRange(0.01, 999_999_999)]
    decimal Amount,
    [property: GSheetColumn("Multiplier")]
    [property: GSheetRange(0, 100)]
    double Multiplier);

/// <summary>
/// Tier 2 config: row validation + graph validation (overlap detection) — all inline.
/// Replaces: RewardRangeValidator + RewardOverlapValidator classes (40+ lines each).
/// </summary>
public class TournamentRewardEntityConfig : GSheetEntityConfig<TournamentRewardEntity>
{
    public override void Configure(GSheetEntityBuilder<TournamentRewardEntity> b)
    {
        b.OrderBy(x => x.TournamentId).ThenBy(x => x.PlaceFrom);

        // Row validation: PlaceTo >= PlaceFrom
        b.Rule(x => x.PlaceTo)
            .Must((entity, placeTo) => placeTo >= entity.PlaceFrom)
            .WithMessage(placeTo => $"PlaceTo ({placeTo}) must be >= PlaceFrom");

        // Graph validation: detect overlapping reward ranges per tournament
        b.ValidateGraph((rows, ctx) =>
        {
            var errors = new List<string>();
            foreach (var group in rows.GroupBy(r => r.TournamentId, StringComparer.Ordinal))
            {
                var ordered = group.OrderBy(r => r.PlaceFrom).ToList();
                for (var i = 1; i < ordered.Count; i++)
                {
                    var prev = ordered[i - 1];
                    var curr = ordered[i];
                    if (curr.PlaceFrom <= prev.PlaceTo)
                        errors.Add($"Overlap in tournament '{group.Key}': " +
                                   $"{prev.PlaceFrom}-{prev.PlaceTo} overlaps with {curr.PlaceFrom}-{curr.PlaceTo}");
                }
            }

            return errors;
        });
    }
}
