using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace CoreLibs.Localization.LiveConfig.Json;

/// <summary>
/// Loads <see cref="LocalizationSnapshot"/> from JSON files.
/// Expected format: one file per culture, named "{culture}.json" (e.g. "ru.json").
/// </summary>
public sealed class LocalizationJsonLoader
{
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<LocalizationJsonLoader> _logger;

    public LocalizationJsonLoader(
        IFileProvider fileProvider,
        ILogger<LocalizationJsonLoader> logger)
    {
        _fileProvider = fileProvider;
        _logger = logger;
    }

    public IEnumerable<LocalizationSnapshot> LoadAll(string directory = LocalizationConstants.DefaultLocalesDirectory)
    {
        var dir = _fileProvider.GetDirectoryContents(directory);
        foreach (var file in dir)
        {
            if (!file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;
            var culture = Path.GetFileNameWithoutExtension(file.Name);
            LocalizationSnapshot? dto = null;
            try
            {
                using var stream = file.CreateReadStream();
                var entries = JsonSerializer.Deserialize<Dictionary<string, LocalizationValue>>(
                    stream, JsonSerializerOptions.Web) ?? [];
                dto = new LocalizationSnapshot { Culture = culture, Entries = entries };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize locale file '{File}'", file.Name);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to read locale file '{File}'", file.Name);
            }

            if (dto is not null)
                yield return dto;
        }
    }
}
