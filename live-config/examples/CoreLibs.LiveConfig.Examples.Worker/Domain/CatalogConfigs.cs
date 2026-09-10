namespace CoreLibs.LiveConfig.Examples.Worker.Domain;

// ──────────────────────────────────────────────
// Currency — base config, no dependencies
// ──────────────────────────────────────────────

public record CurrencyConfig(string Code, string Name, int DecimalPlaces);

// ──────────────────────────────────────────────
// Tournaments — depends on Currency
// ──────────────────────────────────────────────

public record TournamentConfig(
    string TournamentId,
    string Name,
    string Description,
    string EntryCurrency,
    decimal EntryFee,
    int MinPlayers,
    int MaxPlayers,
    string Status,
    IReadOnlyList<TournamentRewardTier> RewardTiers);

public record TournamentRewardTier(
    string TournamentId,
    int PlaceFrom,
    int PlaceTo,
    string RewardCurrency,
    decimal Amount,
    double Multiplier);

// ──────────────────────────────────────────────
// Items — base config for item catalog
// ──────────────────────────────────────────────

public record ItemConfig(
    string ItemId,
    string Name,
    string Rarity,
    string Category);

// ──────────────────────────────────────────────
// Item Prices — depends on Items + Currency (tightly coupled → transactional)
// ──────────────────────────────────────────────

public record ItemPriceConfig(
    string ItemId,
    string Currency,
    decimal Price,
    decimal? DiscountPrice,
    bool IsActive);

// ──────────────────────────────────────────────
// Item Bundles — depends on Items + ItemPrices (tightly coupled → transactional)
// ──────────────────────────────────────────────

public record ItemBundleConfig(
    string BundleId,
    string Name,
    IReadOnlyList<string> ItemIds,
    string Currency,
    decimal BundlePrice);
