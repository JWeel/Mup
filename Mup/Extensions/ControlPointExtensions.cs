
using System.Windows;

namespace Mup.Extensions
{
    public static class ControlPointExtensions
    {
        #region Deconstruct
            
        public static void Deconstruct(this Point point, out double x, out double y)
        {
            x = point.X;
            y = point.Y;
        }

        #endregion

        #region To Index

        public static int ToIndex(this Point point, int imageWidth) =>
            (int) point.X + ((int) point.Y * imageWidth);

        #endregion
    }
}