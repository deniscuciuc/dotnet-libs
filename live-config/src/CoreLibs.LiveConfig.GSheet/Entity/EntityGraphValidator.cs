using CoreLibs.LiveConfig.GSheet.Pipeline;

namespace CoreLibs.LiveConfig.GSheet.Entity;

/// <summary>
/// Wraps inline graph validators from <see cref="GSheetEntityBuilder{TRow,TDomain}"/>
/// into a standard <see cref="IGraphValidator{TRow}"/>.
/// </summary>
internal sealed class EntityGraphValidator<TRow>(
    List<Func<IReadOnlyList<TRow>, GSheetImportContext, IEnumerable<string>>> validators)
    : IGraphValidator<TRow>
{
    public GSheetValidationResult Validate(IReadOnlyList<TRow> rows, GSheetImportContext context)
    {
        var allErrors = new List<GSheetValidationError>();

        foreach (var messages in validators.Select(validator => validator(rows, context)))
        {
            allErrors.AddRange(messages.Select(msg => context.SheetName is not null
                ? GSheetValidationError.ForSheet(context.SheetName, -1, "Graph", msg)
                : new GSheetValidationError(-1, "Graph", msg)));
        }

        return allErrors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(allErrors);
    }
}
