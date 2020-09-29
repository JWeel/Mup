using Mup.Extensions;
using System.Collections.Generic;
using System.Drawing;

namespace Mup.Helpers
{
    public class ImageInfo
    {
        #region Constructors

        public ImageInfo(Color[] pixels, HashSet<Color> nonEdgeColorSet, int width, int height)
        {
            this.Pixels = pixels;
            this.NonEdgeColorSet = nonEdgeColorSet;
            this.Width = width;
            this.Height = height;
        }

        #endregion

        #region Properties

        public Color[] Pixels { get; }

        public HashSet<Color> NonEdgeColorSet { get; }

        public int Width { get; }

        public int Height { get; }

        #endregion
        
        #region Methods

        public Color Locate(int x, int y)
        {
            var index = new Point(x, y).ToIndex(this.Width);
            return this.Pixels[index];
        }
            
        #endregion
    }
}