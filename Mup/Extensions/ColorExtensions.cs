using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Mup.Extensions
{
    public static class ColorExtensions
    {
        #region Deconstruct

        public static void Deconstruct(this Color color, out byte r, out byte g, out byte b, out byte a)
        {
            r = color.R;
            g = color.G;
            b = color.B;
            a = color.A;
        }

        #endregion

        #region Print

        public static string Print(this Color color)
        {
            var (r, g, b, a) = color;
            return $"Color: {r}-{g}-{b} ({r:X2}{g:X2}{b:X2})";
        }

        public static string PrintHex(this Color color)
        {
            var (r, g, b, a) = color;
            return $"{r:X2}{g:X2}{b:X2}";
        }

        #endregion

        #region Argb Is In

        public static bool ArgbIsIn(this Color color, ISet<int> argbSet) =>
            color.ToArgb().In(argbSet);

        public static bool ArgbIsNotIn(this Color color, ISet<int> argbSet) =>
            !color.ArgbIsIn(argbSet);

        public static IEnumerable<Color> WhereArgbNotIn(this IEnumerable<Color> source, ISet<int> argbSet) =>
            source.Where(color => color.ArgbIsNotIn(argbSet));

        #endregion
    }
}