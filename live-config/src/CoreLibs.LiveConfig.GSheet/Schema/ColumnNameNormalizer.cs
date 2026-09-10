using System.Text;

namespace CoreLibs.LiveConfig.GSheet.Schema;

/// <summary>
/// Normalizes column names for flexible header matching across naming conventions.
/// Strips underscores, hyphens, and spaces, then lowercases — so
/// <c>CoinsAmount</c>, <c>coins_amount</c>, <c>coinsAmount</c>, <c>coinsamount</c>,
/// <c>coins-amount</c>, and <c>Coins Amount</c> all normalize to <c>coinsamount</c>.
/// </summary>
public static class ColumnNameNormalizer
{
    /// <summary>
    /// Normalizes a column or property name by stripping separators and lowercasing.
    /// </summary>
    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var sb = new StringBuilder(name.Length);
        foreach (var c in name.Where(c => c is not ('_' or '-' or ' ')))
        {
            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
