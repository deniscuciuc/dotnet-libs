using System.Text.Json;
using CoreLibs.LiveConfig;
using Microsoft.Extensions.Logging;

namespace CoreLibs.Localization.LiveConfig;

public sealed class LocalizationConfigApplier : IConfigApplier
{
    private readonly ILocalizationCache _cache;

    private readonly ILogger<LocalizationConfigApplier> _logger;

    public LocalizationConfigApplier(
        ILocalizationCache cache,
        ILogger<LocalizationConfigApplier> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public required string ConfigType { get; init; }

    public Task ApplyAsync(string json, int version, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<LocalizationSnapshot>(json,
                JsonSerializerOptions.Web);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Culture))
            {
                _logger.LogWarning("Received null or invalid LocalizationSnapshot for config type '{Type}'",
                    ConfigType);
                return Task.CompletedTask;
            }

            _cache.Update(dto.Culture, dto.Entries);
            _logger.LogInformation(
                "Localization cache updated: culture='{Culture}', entries={Count}, version={Version}",
                dto.Culture, dto.Entries.Count, version);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize LocalizationSnapshot for '{Type}'", ConfigType);
            throw;
        }

        return Task.CompletedTask;
    }
}
