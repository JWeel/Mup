using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;

namespace Mup.Helpers
{
    public class Generate
    {
        #region Range

        public static IEnumerable<int> Range(int end) =>
            Generate.Range(0, end);

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
            // color values between 100-240
            var r = RandomNumberGenerator.GetInt32(141) + 100;
            var g = RandomNumberGenerator.GetInt32(141) + 100;
            var b = RandomNumberGenerator.GetInt32(141) + 100;
            return Color.FromArgb(r, g, b);
        }

        public static Color MupColor(ISet<Color> existingColors)
        {
            Color color;
            do color = Generate.MupColor();
            while (existingColors.Contains(color));
            return color;
        }

        #endregion
    }
}