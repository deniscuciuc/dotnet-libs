namespace CoreLibs.LiveConfig;

/// <summary>
/// Controls how a batch of config imports is applied.
/// </summary>
public enum ImportTransactionMode
{
    /// <summary>
    /// Default. Each config imports independently.
    /// Failures don't block others — partial success is allowed.
    /// Best for distributed systems with eventually consistent configs.
    /// </summary>
    Partial,

    /// <summary>
    /// All-or-nothing. If any config in the batch fails to import,
    /// none are applied (store + distributor are not updated).
    /// Use for tightly coupled configs that must stay in sync
    /// (e.g. Items + ItemPrices + ItemBundles).
    /// </summary>
    Transactional
}
