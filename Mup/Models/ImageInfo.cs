using Mup.Extensions;
using System.Collections.Generic;
using System.Drawing;

namespace Mup.Models
{
    public class ImageInfo
    {
        #region Constructors

        public ImageInfo(Color[] pixels, ISet<Color> nonEdgeColorSet, IDictionary<Color, int> sizeByColor, int width, int height)
        {
            this.Pixels = pixels;
            this.NonEdgeColorSet = nonEdgeColorSet;
            this.SizeByColor = sizeByColor;
            this.Width = width;
            this.Height = height;
        }

        #endregion

        #region Properties

        public Color[] Pixels { get; }

        public ISet<Color> NonEdgeColorSet { get; }

        public IDictionary<Color, int> SizeByColor { get; }

        public int Width { get; }

        public int Height { get; }

        #endregion

        #region Methods

        public Color Locate(int x, int y)
        {
            var index = new Point(x, y).ToIndex(this.Width);
            if ((index < 0) || (index >= this.Pixels.Length))
                return default;
            return this.Pixels[index];
        }

        #endregion
    }
}