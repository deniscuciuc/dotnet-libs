using System.Linq.Expressions;
using System.Reflection;

namespace CoreLibs.LiveConfig.GSheet.Entity;

/// <summary>
/// Marker interface for type-erased property rules.
/// </summary>
internal interface IPropertyRule<in TRow>
{
    GSheetValidationResult Validate(IReadOnlyList<TRow> rows, string? sheetName);
}

/// <summary>
/// FluentValidation-style property rule builder.
/// </summary>
public sealed class PropertyRuleBuilder<TRow, TProp> : IPropertyRule<TRow>
{
    private readonly Func<TRow, TProp> _propertyGetter;
    private readonly string _propertyName;
    private Func<TRow, TProp, bool>? _predicate;
    private string? _message;
    private Func<TProp, string>? _messageFactory;

    internal PropertyRuleBuilder(object parent, Expression<Func<TRow, TProp>> propertySelector)
    {
        _propertyGetter = propertySelector.Compile();
        _propertyName = ExtractPropertyName(propertySelector);

        // Register this rule with the parent builder via internals
        if (parent is GSheetEntityBuilder<TRow, object?> typed)
        {
            typed.PropertyRules.Add(this);
        }
        else
        {
            // Use reflection to add to the PropertyRules list via the base class
            var propRulesField = parent.GetType()
                .GetProperty("PropertyRules", BindingFlags.NonPublic | BindingFlags.Instance);
            if (propRulesField?.GetValue(parent) is System.Collections.IList list) list.Add(this);
        }
    }

    /// <summary>
    /// Adds a property-only predicate. Returns false for invalid values.
    /// </summary>
    public PropertyRuleBuilder<TRow, TProp> Must(Func<TProp, bool> predicate)
    {
        _predicate = (_, prop) => predicate(prop);
        return this;
    }

    /// <summary>
    /// Adds a row-context predicate. Returns false for invalid values.
    /// </summary>
    public PropertyRuleBuilder<TRow, TProp> Must(Func<TRow, TProp, bool> predicate)
    {
        _predicate = predicate;
        return this;
    }

    /// <summary>
    /// Sets the error message for this rule.
    /// </summary>
    public PropertyRuleBuilder<TRow, TProp> WithMessage(string message)
    {
        _message = message;
        return this;
    }

    /// <summary>
    /// Sets a dynamic error message factory for this rule.
    /// </summary>
    public PropertyRuleBuilder<TRow, TProp> WithMessage(Func<TProp, string> messageFactory)
    {
        _messageFactory = messageFactory;
        return this;
    }

    GSheetValidationResult IPropertyRule<TRow>.Validate(IReadOnlyList<TRow> rows, string? sheetName)
    {
        if (_predicate is null) return GSheetValidationResult.Ok();

        var errors = new List<GSheetValidationError>();
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var value = _propertyGetter(row);

            if (_predicate(row, value)) continue;

            var reason = _messageFactory?.Invoke(value)
                         ?? _message
                         ?? $"Validation failed for '{_propertyName}'";

            errors.Add(sheetName is not null
                ? GSheetValidationError.ForSheet(sheetName, i, _propertyName, reason)
                : new GSheetValidationError(i, _propertyName, reason));
        }

        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }

    private static string ExtractPropertyName(Expression<Func<TRow, TProp>> expression)
    {
        return expression.Body switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression unaryMember } => unaryMember.Member.Name,
            _ => "Unknown"
        };
    }
}
