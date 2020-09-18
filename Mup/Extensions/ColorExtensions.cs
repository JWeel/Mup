using System.Collections.Generic;
using System.Drawing;

namespace Mup.Extensions
{
    public static class ColorExtensions
    {
        #region To Bytes

        public static IEnumerable<byte> ToBytes(this Color color)
        {
            yield return color.B;
            yield return color.G;
            yield return color.R;
            yield return color.A;
        }

        #endregion

        #region Is Black Or White

        private const int WHITE_ARGB = unchecked((int) 0xFFFFFFFF);
        private const int BLACK_ARGB = unchecked((int) 0xFF000000);

        public static bool IsBlackOrWhite(this Color color) =>
            color.ToArgb().Into(x => (x == BLACK_ARGB) || (x == WHITE_ARGB));

        #endregion
    }
}