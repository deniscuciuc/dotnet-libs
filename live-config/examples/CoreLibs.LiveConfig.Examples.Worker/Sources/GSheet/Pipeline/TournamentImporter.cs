using CoreLibs.LiveConfig.Examples.Worker.Domain;
using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.Pipeline;

// ═══════════════════════════════════════════════════════════════
// Tournament Reward Rows — flat sheet with one row per reward tier
// Sheet: TournamentRewards
// Columns: TournamentId | PlaceFrom | PlaceTo | RewardCurrency | Amount | Multiplier
// ═══════════════════════════════════════════════════════════════

public record TournamentRewardRow(
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
    [property: GSheetRef(typeof(CurrencyConfig), nameof(CurrencyConfig.Code))]
    string RewardCurrency,
    [property: GSheetColumn("Amount")]
    [property: GSheetRequired]
    [property: GSheetRange(0.01, 999_999_999)]
    decimal Amount,
    [property: GSheetColumn("Multiplier")]
    [property: GSheetRange(0, 100)]
    double Multiplier);

// Reward rows are imported first (no aggregation — raw tiers stored in context)
[GSheetImporter("TournamentRewards", "A1:F", Needs = [typeof(CurrencyImporter)])]
public sealed class TournamentRewardImporter : IGSheetPipeline<TournamentRewardRow, TournamentRewardTier>
{
    public IRowValidator<TournamentRewardRow> RowValidator => new RewardRangeValidator();

    public IGraphValidator<TournamentRewardRow> GraphValidator => new RewardOverlapValidator();

    public IEnumerable<TournamentRewardTier> MapToDomain(
        IReadOnlyList<TournamentRewardRow> rows,
        GSheetImportContext context)
    {
        return rows
            .OrderBy(r => r.TournamentId, StringComparer.Ordinal)
            .ThenBy(r => r.PlaceFrom)
            .Select(r => new TournamentRewardTier(
                r.TournamentId, r.PlaceFrom, r.PlaceTo, r.RewardCurrency, r.Amount, r.Multiplier));
    }
}

public sealed class RewardRangeValidator : IRowValidator<TournamentRewardRow>
{
    public GSheetValidationResult Validate(IReadOnlyList<TournamentRewardRow> rows, GSheetImportContext context)
    {
        var errors = new List<GSheetValidationError>();

        for (var i = 0; i < rows.Count; i++)
            if (rows[i].PlaceTo < rows[i].PlaceFrom)
                errors.Add(new GSheetValidationError(i, nameof(TournamentRewardRow.PlaceTo),
                    $"PlaceTo ({rows[i].PlaceTo}) must be >= PlaceFrom ({rows[i].PlaceFrom})"));

        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }
}

// ═══════════════════════════════════════════════════════════════
// Tournament Rows — master sheet with one row per tournament
// Sheet: Tournaments
// Columns: TournamentId | Name | Description | EntryCurrency | EntryFee | MinPlayers | MaxPlayers | Status
// ═══════════════════════════════════════════════════════════════

public record TournamentRow(
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
    [property: GSheetRef(typeof(CurrencyConfig), nameof(CurrencyConfig.Code))]
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

// ── Pipeline: Tournaments aggregate reward tiers from TournamentRewards ──

[GSheetImporter("Tournaments", "A1:H", Needs = [typeof(TournamentRewardImporter), typeof(CurrencyImporter)])]
public sealed class TournamentImporter : IGSheetPipeline<TournamentRow, TournamentConfig>
{
    public IRowValidator<TournamentRow> RowValidator => new TournamentRowValidator();

    public IGraphValidator<TournamentRow> GraphValidator => new TournamentOrphanValidator();

    public IAggregator<TournamentRow, TournamentConfig> Aggregator => new TournamentAggregator();
}

// ── Cross-cutting aggregator: joins Tournaments + TournamentRewards ──

public sealed class TournamentAggregator : IAggregator<TournamentRow, TournamentConfig>
{
    public IEnumerable<TournamentConfig> Aggregate(
        IReadOnlyList<TournamentRow> rows,
        GSheetImportContext context)
    {
        // Use domain output from TournamentRewardImporter — single source of truth, no re-mapping
        var rewardsByTournament = context.GetOptionalDomain<TournamentRewardTier>()
            .GroupBy(r => r.TournamentId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<TournamentRewardTier>)g.ToList(), StringComparer.Ordinal);

        return rows
            .OrderBy(r => r.TournamentId, StringComparer.Ordinal)
            .Select(row =>
            {
                var tiers = rewardsByTournament.TryGetValue(row.TournamentId, out var rr)
                    ? rr
                    : [];

                return new TournamentConfig(
                    row.TournamentId,
                    row.Name,
                    row.Description,
                    row.EntryCurrency,
                    row.EntryFee,
                    row.MinPlayers,
                    row.MaxPlayers,
                    row.Status,
                    tiers);
            });
    }
}

// ── Custom validator ──────────────────────────────────────────

public sealed class TournamentRowValidator : IRowValidator<TournamentRow>
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "Active", "Paused", "Completed", "Cancelled"
    };

    public GSheetValidationResult Validate(IReadOnlyList<TournamentRow> rows, GSheetImportContext context)
    {
        var errors = new List<GSheetValidationError>();

        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].MaxPlayers < rows[i].MinPlayers)
                errors.Add(new GSheetValidationError(i, nameof(TournamentRow.MaxPlayers),
                    $"MaxPlayers ({rows[i].MaxPlayers}) must be >= MinPlayers ({rows[i].MinPlayers})"));

            if (!ValidStatuses.Contains(rows[i].Status))
                errors.Add(new GSheetValidationError(i, nameof(TournamentRow.Status),
                    $"Invalid status '{rows[i].Status}'. Valid: {string.Join(", ", ValidStatuses)}"));
        }

        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }
}

// ── Cross-row validator: detects overlapping reward ranges per tournament ──

public sealed class RewardOverlapValidator : IGraphValidator<TournamentRewardRow>
{
    public GSheetValidationResult Validate(IReadOnlyList<TournamentRewardRow> rows, GSheetImportContext context)
    {
        var errors = new List<GSheetValidationError>();

        foreach (var group in rows.GroupBy(r => r.TournamentId, StringComparer.Ordinal))
        {
            var ordered = group.OrderBy(r => r.PlaceFrom).ToList();

            for (var i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var curr = ordered[i];

                if (curr.PlaceFrom <= prev.PlaceTo)
                    errors.Add(new GSheetValidationError(
                        -1,
                        nameof(TournamentRewardRow.PlaceFrom),
                        $"Overlap in tournament '{group.Key}': " +
                        $"{prev.PlaceFrom}-{prev.PlaceTo} overlaps with {curr.PlaceFrom}-{curr.PlaceTo}",
                        context.SheetName));
            }
        }

        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }
}

// ── Cross-sheet validator: detects orphan rewards for nonexistent tournaments ──

public sealed class TournamentOrphanValidator : IGraphValidator<TournamentRow>
{
    public GSheetValidationResult Validate(IReadOnlyList<TournamentRow> rows, GSheetImportContext context)
    {
        var errors = new List<GSheetValidationError>();

        var tournamentIds = rows.Select(r => r.TournamentId).ToHashSet(StringComparer.Ordinal);
        var rewardRows = context.GetRows<TournamentRewardRow>() ?? [];

        foreach (var reward in rewardRows)
            if (!tournamentIds.Contains(reward.TournamentId))
                errors.Add(new GSheetValidationError(
                    -1,
                    nameof(TournamentRewardRow.TournamentId),
                    $"Orphan reward for unknown tournament '{reward.TournamentId}'",
                    context.SheetName));

        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }
}
