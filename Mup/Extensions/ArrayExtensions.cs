using System;
using System.Linq;

namespace Mup.Extensions
{
    public static class ArrayExtensions
    {
        #region Shuffled

        public static T[] Shuffled<T>(this T[] source)
        {
            var random = new Random();
            var copy = source.ToArray();
            int n = copy.Length;
            while (n > 1)
            {
                n--;
                var k = random.Next(n + 1);
                var value = copy[k];
                copy[k] = copy[n];
                copy[n] = value;
            }
            return copy;
        }
            
        #endregion
    }
}