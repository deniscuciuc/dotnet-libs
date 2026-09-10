using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.EntityApi;

// ═══════════════════════════════════════════════════════════════
// Tier 3 — Full Config with Relationships (separate TRow → TDomain)
// Compare with: Importers/TournamentImporter.cs (TournamentImporter + TournamentAggregator
//               + TournamentRowValidator + TournamentOrphanValidator = ~100 lines)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Tier 3 row type — parsed from the "Tournaments" sheet.
/// </summary>
public record TournamentEntityRow(
    [property: GSheetColumn("TournamentId")]
    [property: GSheetRequired]
    string TournamentId,
    [property: GSheetColumn("Name")]
    [property: GSheetRequired]
    string Name,
    [property: GSheetColumn("Description")]
    string Description,
    [property: GSheetColumn("EntryCurrency")]
    [property: GSheetRequired]
    [property: GSheetRef(typeof(CurrencyEntity), nameof(CurrencyEntity.Code))]
    string EntryCurrency,
    [property: GSheetColumn("EntryFee")]
    [property: GSheetRequired]
    [property: GSheetRange(0, 999_999)]
    decimal EntryFee,
    [property: GSheetColumn("MinPlayers")]
    [property: GSheetRequired]
    [property: GSheetRange(2, 10000)]
    int MinPlayers,
    [property: GSheetColumn("MaxPlayers")]
    [property: GSheetRequired]
    [property: GSheetRange(2, 100000)]
    int MaxPlayers,
    [property: GSheetColumn("Status")]
    [property: GSheetRequired]
    string Status);

/// <summary>
/// Tier 3 domain type — aggregated from TournamentEntityRow + TournamentRewardEntity children.
/// Re-uses the existing TournamentConfig from Configs.cs.
/// </summary>
public record TournamentEntity(
    string TournamentId,
    string Name,
    string Description,
    string EntryCurrency,
    decimal EntryFee,
    int MinPlayers,
    int MaxPlayers,
    string Status,
    IReadOnlyList<TournamentRewardEntity> RewardTiers);

/// <summary>
/// Tier 3 config: separate TRow + TDomain, HasMany relationship, custom mapping + validation.
/// Replaces: TournamentImporter + TournamentAggregator + TournamentRowValidator + TournamentOrphanValidator.
/// </summary>
public class TournamentEntityConfig : GSheetEntityConfig<TournamentEntityRow, TournamentEntity>
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "Active", "Paused", "Completed", "Cancelled"
    };

    public override void Configure(GSheetEntityBuilder<TournamentEntityRow, TournamentEntity> b)
    {
        b.Sheet("Tournaments", "A1:H");
        b.HasKey(x => x.TournamentId);

        // Row validation
        b.Rule(x => x.MaxPlayers)
            .Must((row, max) => max >= row.MinPlayers)
            .WithMessage(max => $"MaxPlayers ({max}) must be >= MinPlayers");

        b.Rule(x => x.Status)
            .Must(ValidStatuses.Contains)
            .WithMessage(s => $"Invalid status '{s}'. Valid: {string.Join(", ", ValidStatuses)}");

        // Graph validation: orphan rewards for nonexistent tournaments
        b.ValidateGraph((rows, ctx) =>
        {
            var tournamentIds = rows.Select(r => r.TournamentId).ToHashSet(StringComparer.Ordinal);
            var rewards = ctx.GetOptionalDomain<TournamentRewardEntity>();
            return rewards
                .Where(r => !tournamentIds.Contains(r.TournamentId))
                .Select(r => $"Reward for tournament '{r.TournamentId}' has no matching tournament");
        });

        // HasMany: aggregate reward tiers from the TournamentRewardEntity pipeline
        b.HasMany<TournamentRewardEntity>()
            .WithForeignKey(r => r.TournamentId)
            .Ordered(r => r.PlaceFrom);

        b.OrderBy(x => x.TournamentId);

        // Map: row + children → domain
        b.Map((row, ctx) => new TournamentEntity(
            row.TournamentId,
            row.Name,
            row.Description,
            row.EntryCurrency,
            row.EntryFee,
            row.MinPlayers,
            row.MaxPlayers,
            row.Status,
            ctx.Children<TournamentRewardEntity>(row.TournamentId)));
    }
}
