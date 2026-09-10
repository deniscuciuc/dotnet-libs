using CoreLibs.LiveConfig.GSheet.Schema;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.GSheet;

public class GSheetParserService(GSheetOptions options, GSheetConnector connector, ILogger<GSheetParserService> logger)
{
    private Dictionary<string, IList<IList<object>>>? _cachedData;

    public async Task PreloadAllSheetsAsync(
        IEnumerable<(string SheetName, string Range)> sheetRanges,
        CancellationToken cancellationToken = default)
    {
        var sheetRangesList = sheetRanges.ToList();
        logger.LogInformation("Starting preload of {Count} sheet ranges", sheetRangesList.Count);

        var ranges = sheetRangesList.Select(sr =>
        {
            var fullRange = string.IsNullOrWhiteSpace(sr.Range)
                ? sr.SheetName
                : string.IsNullOrWhiteSpace(sr.SheetName)
                    ? sr.Range
                    : $"{sr.SheetName}!{sr.Range}";
            return fullRange;
        }).ToList();

        var response = await connector.BatchGetAsync(ranges, cancellationToken);
        _cachedData = new Dictionary<string, IList<IList<object>>>();

        for (var i = 0; i < ranges.Count; i++)
            if (response.ValueRanges[i].Values != null)
            {
                _cachedData[ranges[i]] = response.ValueRanges[i].Values;
                logger.LogInformation("Cached data for '{Range}': {RowCount} rows",
                    ranges[i], response.ValueRanges[i].Values.Count);
            }
            else
            {
                logger.LogWarning("No values returned for range '{Range}'", ranges[i]);
            }

        logger.LogInformation("Preload completed. Total cached ranges: {Count}", _cachedData.Count);
    }

    public async Task<IReadOnlyList<T>> ParseAsync<T>(
        string sheetName,
        string range,
        ClassMap<T> mapper,
        CancellationToken cancellationToken = default)
    {
        var fullRange = string.IsNullOrWhiteSpace(range)
            ? sheetName
            : string.IsNullOrWhiteSpace(sheetName)
                ? range
                : $"{sheetName}!{range}";

        if (_cachedData != null && _cachedData.TryGetValue(fullRange, out var values))
        {
            logger.LogDebug("Cache HIT for '{FullRange}'", fullRange);
        }
        else
        {
            logger.LogWarning("Cache MISS for '{FullRange}' - falling back to individual API request", fullRange);
            var request = connector.CreateGetRequest(sheetName, range);
            var response = await request.ExecuteAsync(cancellationToken);
            values = response.Values;
        }

        var delimiter = options.Csv.Delimiter;
        var lines = new List<string>();
        if (values != null)
            lines.AddRange(values.Select(row => row.Select(v => v?.ToString() ?? string.Empty))
                .Select(cells => string.Join(delimiter, cells.Select(cell => CsvEscapeCell(cell, delimiter)))));

        var csvText = string.Join("\n", lines);

        using var reader = new StringReader(csvText);
        var config = new CsvConfiguration(options.Csv.ResolveCulture())
        {
            Delimiter = delimiter,
            PrepareHeaderForMatch = args => ColumnNameNormalizer.Normalize(args.Header),
            ShouldUseConstructorParameters = args =>
                args.ParameterType.GetConstructor(Type.EmptyTypes) is null
        };

        if (options.Csv.IgnoreHeaderValidated) config.HeaderValidated = null;
        if (options.Csv.IgnoreMissingField) config.MissingFieldFound = null;

        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap(mapper);

        var records = csv.GetRecords<T>().ToList();
        logger.LogInformation("Parsed {RecordCount} records of type {TypeName} from '{FullRange}'",
            records.Count, typeof(T).Name, fullRange);

        return records;
    }

    private static string CsvEscapeCell(string cell, string delimiter)
    {
        var needsQuoting = cell.Contains('"') || cell.Contains(delimiter)
                           || cell.Contains('\n') || cell.Contains('\r');
        if (!needsQuoting)
            return cell;
        return $"\"{cell.Replace("\"", "\"\"")}\"";
    }
}
