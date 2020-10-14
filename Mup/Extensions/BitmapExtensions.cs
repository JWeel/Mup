using Mup.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

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

        public static byte[] ToPNG(this Bitmap bitmap)
        {
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        #endregion

        #region To BitmapImage

        public static BitmapImage ToBitmapImage(this byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                return image;
            }
        }
            
        #endregion

        #region To Pixel

        public static Color[] ToPixelColors(this byte[] bytes) =>
            Generate.Range(0, bytes.Length, step: 4)
                .Select(i => Color.FromArgb(bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i]))
                .ToArray();

        public static Dictionary<Color, Point[]> MapPointsByColor(this IEnumerable<(int Index, Color Color)> pixels, int imageWidth) =>
            pixels
                .GroupBy(x => x.Color)
                .Select(group => (Color: group.Key, Points: group.ToArray(x => x.Index.ToPoint(imageWidth))))
                .ToDictionary(x => x.Color, x => x.Points);

        #endregion

        #region Is Edge Color

        private const int WHITE_ARGB = unchecked((int) 0xFFFFFFFF);
        private const int BLACK_ARGB = unchecked((int) 0xFF000000);
        private const int TRANS_BLACK_ARGB = 0;
        private const int TRANS_WHITE_ARGB = unchecked((int) 0x00FFFFFF);

        public static bool IsWhite(this Color color) =>
            (color.ToArgb() == WHITE_ARGB);

        public static bool IsBlack(this Color color) =>
            (color.ToArgb() == BLACK_ARGB);
        
        public static bool IsTransparent(this Color color) =>
            (color.ToArgb() == TRANS_WHITE_ARGB) || (color.ToArgb() == TRANS_BLACK_ARGB);

        public static bool IsEdgeColor(this Color color) =>
            color.ToArgb().Into(x => 
                (x == BLACK_ARGB) || (x == WHITE_ARGB) || (x == TRANS_BLACK_ARGB) || (x == TRANS_WHITE_ARGB));

        #endregion

        #region Save To Image

        public static void SaveToImage(this byte[] bytes, string filePath)
        {
            using var stream = new MemoryStream(bytes);
            using var image = Image.FromStream(stream);
            image.Save(filePath);
        }
            
        #endregion
    }
}