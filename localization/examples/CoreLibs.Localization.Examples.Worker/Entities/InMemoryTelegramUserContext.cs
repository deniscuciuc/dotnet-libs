using System.Collections.Concurrent;
using CoreLibs.Localization.Telegram;

namespace CoreLibs.Localization.Examples.Worker.Entities;

/// <summary>
/// Thread-safe in-memory implementation of ITelegramUserContext
/// Maps Telegram user IDs to their preferred culture code.
/// In a real application this would be backed by a database.
/// </summary>
public sealed class InMemoryTelegramUserContext : ITelegramUserContext
{
    private readonly ConcurrentDictionary<long, string> _preferences = new();

    public ValueTask<string?> GetUserCultureAsync(long userId) =>
        ValueTask.FromResult<string?>(_preferences.GetValueOrDefault(userId));

    public void Set(long userId, string culture)
    {
        _preferences[userId] = culture;
    }

    public void Remove(long userId)
    {
        _preferences.TryRemove(userId, out _);
    }

    /// <summary>All configured user → culture mappings (snapshot).</summary>
    public IReadOnlyDictionary<long, string> All => _preferences;
}
