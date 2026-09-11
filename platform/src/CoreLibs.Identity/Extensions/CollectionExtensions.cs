namespace CoreLibs.Identity.Extensions;

public static class CollectionExtensions
{
    public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T>? source)
    {
        return source == null || source.Count == 0;
    }

    public static void AddRange<T>(this HashSet<T>? source, IEnumerable<T> items)
    {
        if (source != null)
            foreach (var item in items)
                source.Add(item);
        else
            throw new ArgumentNullException(nameof(source));
    }
}
