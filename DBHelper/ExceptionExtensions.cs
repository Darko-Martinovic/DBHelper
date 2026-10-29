namespace DBHelper;

/// <summary>
/// When working with SMO, when an exception arises,
/// usually, the most inner one contains the information that you need.
/// In order to collect all exception text, you have to travel through the exception hierarchy.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>Walks a chain of items, such as an exception and its inner exceptions.</summary>
    public static IEnumerable<TSource> CollectThemAll<TSource>(
        this TSource source,
        Func<TSource, TSource> nextItem,
        Func<TSource, bool> canContinue)
    {
        for (var current = source; canContinue(current); current = nextItem(current))
        {
            yield return current;
        }
    }

    /// <summary>Walks a chain of items until the next one is null.</summary>
    public static IEnumerable<TSource> CollectThemAll<TSource>(
        this TSource source,
        Func<TSource, TSource?> nextItem)
        where TSource : class
    {
        return CollectThemAll<TSource?>(source, s => nextItem(s!), s => s != null).Select(s => s!);
    }

    /// <summary>The messages of an exception and all its inner exceptions, one per line.</summary>
    public static string AllMessages(this Exception exception) =>
        string.Join(Environment.NewLine + "\t", exception.CollectThemAll(e => e.InnerException).Select(e => e.Message));
}
