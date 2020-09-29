using System.Collections.Generic;
using System.Linq;

namespace Mup.Extensions
{
    public static class ListExtensions
    {
        #region Pop

        public static T Pop<T>(this IList<T> source)
        {
            var value = source.First();
            source.RemoveAt(0);
            return value;
        }

        public static bool TryPop<T>(this IList<T> source, out T value)
        {
            if (!source.Any())
            {
                value = default;
                return false;
            }
            value = source.Pop();
            return true;
        }

        public static T PopRandom<T>(this IList<T> source)
        {
            var (index, value) = source.RandomWithIndex();
            source.RemoveAt(index);
            return value;
        }

        public static bool TryPopRandom<T>(this IList<T> source, out T value)
        {
            if (!source.Any())
            {
                value = default;
                return false;
            }
            value = source.PopRandom();
            return true;
        }

        #endregion
    }
}