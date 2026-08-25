
namespace Goose
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> SkipFirstMatching<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException("source");

            if (predicate is null)
                throw new ArgumentNullException("predicate");

            return SkipFirstMatchingCore(source, predicate);
        }

        private static IEnumerable<T> SkipFirstMatchingCore<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            bool itemToSkipSeen = false;

            foreach (var item in source)
            {
                if (!itemToSkipSeen && predicate(item))
                    itemToSkipSeen = true;
                else
                    yield return item;
            }
        }
    }
}