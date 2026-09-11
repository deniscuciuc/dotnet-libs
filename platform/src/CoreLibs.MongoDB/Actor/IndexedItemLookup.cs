namespace CoreLibs.MongoDB.Actor;

public static class IndexedItemLookup
{
    public static IndexedItem<T>? IndexedItem<T>(this IList<T> list, Func<T, bool> predicate)
    {
        for (var i = 0; i < list.Count; i++)
            if (predicate(list[i]))
                return new IndexedItem<T>(list[i], i);

        return null;
    }

    public static IndexedItem<T> IndexedItem<T>(this IList<T> list, T value)
    {
        var index = list.IndexOf(value);
        if (index < 0)
            throw new ArgumentException("The value is not in the list.", nameof(value));

        return new IndexedItem<T>(value, index);
    }
}
