namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Chains multiple <see cref="IGraphValidator{TRow}"/> instances, merging their results.
/// </summary>
public sealed class CompositeGraphValidator<TRow> : IGraphValidator<TRow>
{
    private readonly List<IGraphValidator<TRow>> _validators = [];

    public void Add(IGraphValidator<TRow> validator)
    {
        _validators.Add(validator);
    }

    public GSheetValidationResult Validate(IReadOnlyList<TRow> rows, GSheetImportContext context)
    {
        switch (_validators.Count)
        {
            case 0:
                return GSheetValidationResult.Ok();
            case 1:
                return _validators[0].Validate(rows, context);
            default:
                {
                    var results = _validators.Select(v => v.Validate(rows, context)).ToArray();
                    return GSheetValidationResult.Merge(results);
                }
        }
    }
}
