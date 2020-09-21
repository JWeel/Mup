using System;
using System.Collections.Generic;
using System.Drawing;

namespace Mup.Helpers
{
    public class Generate
    {
        #region Properties

        private static Random Random { get; } = new Random();

        #endregion

        #region Range

        public static IEnumerable<int> Range(int start, int end) =>
            Generate.Range(start, end, 1);

        public static IEnumerable<int> Range(int start, int end, int step)
        {
            while (start < end)
            {
                yield return start;
                start += step;
            }
        }

        #endregion

        #region Empty

        public static IEnumerable<T> Empty<T>()
        {
            yield break;
        }

        #endregion

        #region Color

        public static Color MupColor()
        {
            var r = Random.Next(100, 241);
            var g = Random.Next(100, 241);
            var b = Random.Next(100, 241);
            return Color.FromArgb(r, g, b);
        }

        #endregion
    }
}