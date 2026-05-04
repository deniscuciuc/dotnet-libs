using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB.Actor;

/// <summary>
/// Actor that tracks individual field changes and generates partial update definitions.
/// </summary>
public abstract class ObservableActor<TEntity>(
    TEntity entity,
    ActorContext context,
    ILogger logger)
    : Actor<TEntity>(entity, context, logger)
    where TEntity : EntityCas
{
    private static readonly ConcurrentDictionary<string, ObservableField<TEntity>> KnownFields = new();

    private readonly List<ObservableField<TEntity>> _updatedFields = [];

    internal IEnumerable<UpdateDefinition<TEntity>> GetUpdates()
    {
        return _updatedFields.Select(x => x.GetUpdateDefinition(Entity));
    }

    protected void OnChanged<TField>(Expression<Func<TEntity, TField>> expression)
    {
        var path = GetFieldPath(expression);
        if (HasCoveredField(path)) return;

        var field = KnownFields.GetOrAdd(path,
            static (x, args) => new ObservableField<TEntity, TField>(x, args.e),
            new { e = expression });

        AddCoveredField(field);
    }

    protected void OnChanged<TItem>(Expression<Func<TEntity, List<TItem>>> expression, int index)
    {
        var path = $"{GetFieldPath(expression)}[{index}]";
        if (HasCoveredField(path)) return;

        var field = KnownFields.GetOrAdd(path, (x, args) =>
        {
            var methodInfo = typeof(List<TItem>).GetMethod("get_Item")
                             ?? throw new InvalidOperationException("Method not found.");
            var methodExpression = Expression.Call(args.e.Body, methodInfo, Expression.Constant(args.index));
            var lambda = Expression.Lambda<Func<TEntity, TItem>>(methodExpression, args.e.Parameters[0]);
            return new ObservableField<TEntity, TItem>(x, lambda);
        }, new { e = expression, index });

        AddCoveredField(field);
    }

    protected void OnChanged<TItem, TProperty>(
        Expression<Func<TEntity, List<TItem>>> listExpression,
        int index,
        Expression<Func<TItem, TProperty>> propertyExpression)
    {
        var propertyPath = $"{GetFieldPath(listExpression)}[{index}].{GetFieldPath(propertyExpression)}";
        if (HasCoveredField(propertyPath)) return;

        var field = KnownFields.GetOrAdd(propertyPath, (x, args) =>
        {
            var methodInfo = typeof(List<TItem>).GetMethod("get_Item")
                             ?? throw new InvalidOperationException();
            var item = Expression.Call(args.list.Body, methodInfo, Expression.Constant(args.index));
            var property = Expression.Property(item, ((MemberExpression)args.property.Body).Member.Name);
            var lambda = Expression.Lambda<Func<TEntity, TProperty>>(property, args.list.Parameters[0]);
            return new ObservableField<TEntity, TProperty>(x, lambda);
        }, new { list = listExpression, index, property = propertyExpression });

        AddCoveredField(field);
    }

    private static string GetFieldPath(LambdaExpression expression)
    {
        var field = expression.Body.ToString();
        var indexOf = field.IndexOf(".", StringComparison.OrdinalIgnoreCase);
        if (indexOf == -1)
            throw new FormatException($"Can not update root entity {expression}");
        if (field.Contains(".get_Item", StringComparison.OrdinalIgnoreCase))
            throw new FormatException($"Can not update entity field in collection {expression}");
        return field[(indexOf + 1)..];
    }

    private bool HasCoveredField(string path)
    {
        return _updatedFields.Any(x =>
        {
            if (string.Equals(path, x.Path, StringComparison.Ordinal)) return true;
            if (!path.StartsWith(x.Path, StringComparison.Ordinal)) return false;
            return path[x.Path.Length] == '.' || path[x.Path.Length] == '[';
        });
    }

    private void AddCoveredField(ObservableField<TEntity> field)
    {
        _updatedFields.RemoveAll(x =>
        {
            if (string.Equals(x.Path, field.Path, StringComparison.Ordinal)) return true;
            if (!x.Path.StartsWith(field.Path, StringComparison.Ordinal)) return false;
            return x.Path[field.Path.Length] == '.' || x.Path[field.Path.Length] == '[';
        });

        _updatedFields.Add(field);
    }
}
