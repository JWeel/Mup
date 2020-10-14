using Mup.Extensions;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Mup.External
{
    public static class VectorExtensions
    {
        #region Deconstruct

        public static void Deconstruct(this Vector vector, out float x, out float y)
        {
            x = vector.X;
            y = vector.Y;
        }

        #endregion

        #region Point

        public static Vector ToVector(this Point point) =>
            new Vector(point.X, point.Y);

        public static Point ToPoint(this Vector vector) =>
            new Point((int) vector.X, (int) vector.Y);

        #endregion

        #region Average

        /// <summary> Computes the average of a sequence of <see cref="Vector"/> values. </summary>
        public static Vector Average(this IEnumerable<Vector> source) =>
            source.Aggregate((Sum: Vector.Zero, Count: 0),
                (aggregate, value) => (aggregate.Sum + value, aggregate.Count + 1),
                result => result.Sum / result.Count);

        #endregion

        #region Distance

        /// <summary> Returns the Euclidean distance between two vectors. </summary>
        public static double Distance(this Vector left, Vector right) =>
            ((left.X - right.X).Pow(2) + (left.Y - right.Y).Pow(2)).Sqrt();

        #endregion
    }
}