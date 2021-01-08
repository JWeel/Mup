using System.Windows;

namespace Mup.Extensions
{
    public static class WindowsPointExtensions
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

        #region To Drawing

        public static System.Drawing.Point ToDrawing(this Point point) =>
            new System.Drawing.Point((int) point.X, (int) point.Y);
            
        #endregion
    }
}