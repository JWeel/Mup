using Mup.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Mup.Extensions
{
    /// <summary> These methods assume 32-bit images and ignore stride. </summary>
    public static class BitmapExtensions
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

        #region To Pixel

        public static Color[] ToPixelColors(this byte[] bytes) =>
            Generate.Range(0, bytes.Length, step: 4)
                .Select(i => Color.FromArgb(bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i]))
                .ToArray();

        public static Dictionary<Color, Point[]> MapPointsByColor(this IEnumerable<KeyValuePair<int, Color>> pixels, int imageWidth) =>
            pixels
                .GroupBy(x => x.Value)
                .Select(group => (Color: group.Key, Points: group.ToArray(x => x.Key.ToPoint(imageWidth))))
                .ToDictionary(x => x.Color, x => x.Points);

        #endregion

        #region To Point/Index

        public static Point ToPoint(this int index, int imageWidth) =>
            new Point(index % imageWidth, index / imageWidth);

        public static int ToIndex(this Point point, int imageWidth) =>
            point.X + (point.Y * imageWidth);

        #endregion

        #region Is Edge Color

        private const int WHITE_ARGB = unchecked((int) 0xFFFFFFFF);
        private const int BLACK_ARGB = unchecked((int) 0xFF000000);
        private const int TRANS_ARGB = 0;

        public static bool IsWhite(this Color color) =>
            (color.ToArgb() == WHITE_ARGB);

        public static bool IsBlack(this Color color) =>
            (color.ToArgb() == BLACK_ARGB);
        
        public static bool IsTransparent(this Color color) =>
            (color.ToArgb() == TRANS_ARGB);

        public static bool IsEdgeColor(this Color color) =>
            color.ToArgb().Into(x => (x == BLACK_ARGB) || (x == WHITE_ARGB) || (x == TRANS_ARGB));

        #endregion
    }
}