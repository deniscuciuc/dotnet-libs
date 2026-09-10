using CoreLibs.LiveConfig.GSheet.Pipeline;

namespace CoreLibs.LiveConfig.GSheet.Entity;

/// <summary>
/// Wraps inline validators and property rules from <see cref="GSheetEntityBuilder{TRow,TDomain}"/>
/// into a standard <see cref="IRowValidator{TRow}"/>.
/// </summary>
internal sealed class EntityRowValidator<TRow>(
    List<Func<IReadOnlyList<TRow>, GSheetImportContext, IEnumerable<string>>> inlineValidators,
    List<IPropertyRule<TRow>> propertyRules)
    : IRowValidator<TRow>
{
    public GSheetValidationResult Validate(IReadOnlyList<TRow> rows, GSheetImportContext context)
    {
        var allErrors = new List<GSheetValidationError>();

        foreach (var messages in inlineValidators.Select(validator => validator(rows, context)))
        {
            allErrors.AddRange(messages.Select(msg => context.SheetName is not null
                ? GSheetValidationError.ForSheet(context.SheetName, -1, "Entity", msg)
                : new GSheetValidationError(-1, "Entity", msg)));
        }

        foreach (var result in propertyRules.Select(rule => rule.Validate(rows, context.SheetName))
                     .Where(result => result.HasErrors))
        {
            allErrors.AddRange(result.Errors);
        }

        return allErrors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(allErrors);
    }
}
