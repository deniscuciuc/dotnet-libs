using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.GSheet;

public class GSheetConnector(SheetsService service, GSheetOptions options, ILogger<GSheetConnector> logger)
{
    public SpreadsheetsResource.ValuesResource.GetRequest CreateGetRequest(string sheetName, string range)
    {
        var fullRange = string.IsNullOrWhiteSpace(sheetName) ? range : $"{sheetName}!{range}";
        logger.LogDebug("Creating individual request for range: {FullRange} (SpreadsheetId: {SpreadsheetId})",
            fullRange, options.SpreadsheetId);
        return service.Spreadsheets.Values.Get(options.SpreadsheetId, fullRange);
    }

    public async Task<BatchGetValuesResponse> BatchGetAsync(
        IEnumerable<string> ranges,
        CancellationToken cancellationToken = default)
    {
        var rangeList = ranges.ToList();
        logger.LogInformation("Executing batch get request for {Count} ranges from SpreadsheetId: {SpreadsheetId}",
            rangeList.Count, options.SpreadsheetId);
        logger.LogDebug("Batch request ranges: [{Ranges}]", string.Join(", ", rangeList));

        var request = service.Spreadsheets.Values.BatchGet(options.SpreadsheetId);
        request.Ranges = rangeList;

        var response = await request.ExecuteAsync(cancellationToken);
        logger.LogInformation("Batch get response received with {Count} value ranges",
            response.ValueRanges?.Count ?? 0);

        if (response.ValueRanges == null) return response;

        foreach (var valueRange in response.ValueRanges)
        {
            var rowCount = valueRange.Values?.Count ?? 0;
            var colCount = valueRange.Values?.FirstOrDefault()?.Count ?? 0;
            logger.LogDebug("Range '{Range}': {RowCount} rows x {ColCount} columns", valueRange.Range, rowCount,
                colCount);
        }

        return response;
    }
}
