using CoreLibs.Localization;
using CoreLibs.Localization.Runtime;

namespace CoreLibs.Localization.UnitTests;

/// <summary>
/// The cache claims lock-free reads with an atomic swap on write. These cover the swap
/// semantics a caller can actually observe: an update never yields a partially-visible
/// culture, and readers running concurrently with writers never see a torn snapshot.
/// </summary>
public sealed class LocalizationCacheConcurrencyTests
{
    [Fact]
    public void Update_ReplacesCultureWholesale_RatherThanMerging()
    {
        var cache = new LocalizationCache();
        cache.Update("en", new Dictionary<string, LocalizationValue>
        {
            ["a"] = new() { Value = "A" },
            ["b"] = new() { Value = "B" }
        });

        cache.Update("en", new Dictionary<string, LocalizationValue> { ["a"] = new() { Value = "A2" } });

        var provider = cache.GetProvider("en");
        Assert.NotNull(provider);
        Assert.True(provider.TryGet("a", out var a));
        Assert.Equal("A2", a!.Value);
        Assert.False(provider.TryGet("b", out _));
    }

    [Fact]
    public void Update_LeavesOtherCulturesUntouched()
    {
        var cache = new LocalizationCache();
        cache.Update("en", new Dictionary<string, LocalizationValue> { ["k"] = new() { Value = "English" } });
        cache.Update("ro", new Dictionary<string, LocalizationValue> { ["k"] = new() { Value = "Romanian" } });

        cache.Update("en", new Dictionary<string, LocalizationValue> { ["k"] = new() { Value = "English v2" } });

        Assert.True(cache.GetProvider("ro")!.TryGet("k", out var ro));
        Assert.Equal("Romanian", ro!.Value);
    }

    [Fact]
    public void GetLoadedCultures_ListsEveryUpdatedCulture()
    {
        var cache = new LocalizationCache();
        cache.Update("en", new Dictionary<string, LocalizationValue>());
        cache.Update("ro", new Dictionary<string, LocalizationValue>());
        cache.Update("ru", new Dictionary<string, LocalizationValue>());

        Assert.Equal(["en", "ro", "ru"], cache.GetLoadedCultures().Order());
    }

    [Fact]
    public void GetProvider_ReturnsNull_ForUnknownCulture()
    {
        Assert.Null(new LocalizationCache().GetProvider("nope"));
    }

    [Fact]
    public void Update_Rejects_NullOrBlankCulture()
    {
        var cache = new LocalizationCache();
        var entries = new Dictionary<string, LocalizationValue>();

        Assert.Throws<ArgumentNullException>(() => cache.Update(null!, entries));
        Assert.Throws<ArgumentException>(() => cache.Update("  ", entries));
        Assert.Throws<ArgumentNullException>(() => cache.Update("en", null!));
    }

    [Fact]
    public async Task ConcurrentUpdates_AcrossCultures_AllSurvive()
    {
        var cache = new LocalizationCache();
        var cultures = Enumerable.Range(0, 50).Select(i => $"c{i}").ToArray();

        await Parallel.ForEachAsync(cultures, (culture, _) =>
        {
            cache.Update(culture, new Dictionary<string, LocalizationValue>
            {
                ["key"] = new() { Value = culture }
            });
            return ValueTask.CompletedTask;
        });

        Assert.Equal(cultures.Length, cache.GetLoadedCultures().Count);
        foreach (var culture in cultures)
        {
            Assert.True(cache.GetProvider(culture)!.TryGet("key", out var value));
            Assert.Equal(culture, value!.Value);
        }
    }

    [Fact]
    public async Task ReadsDuringWrites_NeverObserveAPartialSnapshot()
    {
        var cache = new LocalizationCache();
        cache.Update("en", new Dictionary<string, LocalizationValue> { ["k"] = new() { Value = "v0" } });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        var writer = Task.Run(() =>
        {
            var i = 0;
            while (!cts.IsCancellationRequested)
            {
                cache.Update("en", new Dictionary<string, LocalizationValue>
                {
                    ["k"] = new() { Value = $"v{++i}" }
                });
            }
        });

        var reader = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                var provider = cache.GetProvider("en");
                Assert.NotNull(provider);
                Assert.True(provider.TryGet("k", out var value));
                Assert.StartsWith("v", value!.Value, StringComparison.Ordinal);
            }
        });

        await Task.WhenAll(writer, reader);
    }
}
