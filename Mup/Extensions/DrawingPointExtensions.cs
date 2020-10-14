using Mup.External;
using System.Drawing;

namespace Mup.Extensions
{
    public static class DrawingPointExtensions
    {
        #region Deconstruct
            
        public static void Deconstruct(this Point point, out int x, out int y)
        {
            x = point.X;
            y = point.Y;
        }

        #endregion

        #region To Point/Index

        public static Point ToPoint(this int index, int imageWidth) =>
            new Point(index % imageWidth, index / imageWidth);

        public static int ToIndex(this Point point, int imageWidth) =>
            point.X + (point.Y * imageWidth);

        #endregion
    }
}