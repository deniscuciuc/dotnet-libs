using System.Linq.Expressions;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB.Actor;

internal abstract class ObservableField<TEntity>(string path)
{
    public string Path { get; } = path;

    public abstract UpdateDefinition<TEntity> GetUpdateDefinition(TEntity entity);
}

internal class ObservableField<TEntity, TField> : ObservableField<TEntity>
{
    private readonly Expression<Func<TEntity, TField>> _expression;
    private readonly Func<TEntity, TField> _delegate;

    public ObservableField(string path, Expression<Func<TEntity, TField>> expression)
        : base(path)
    {
        _expression = expression;
        _delegate = _expression.Compile();
    }

    public override UpdateDefinition<TEntity> GetUpdateDefinition(TEntity entity)
    {
        return Builders<TEntity>.Update.Set(_expression, _delegate.Invoke(entity));
    }
}
