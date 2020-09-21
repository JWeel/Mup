using System.Drawing;

namespace Mup.Extensions
{
    public static class PointExtensions
    {
        #region Deconstruct
            
        public static void Deconstruct(this Point point, out double x, out double y)
        {
            x = point.X;
            y = point.Y;
        }

        #endregion
    }
}