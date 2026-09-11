namespace CoreLibs.MongoDB.Actor;

public class IndexedItem<T>
{
    internal IndexedItem(T value, int index)
    {
        Value = value;
        Index = index;
    }

    public T Value { get; }
    public int Index { get; }
}
