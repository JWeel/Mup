using System.Drawing;

namespace Mup.Models
{
    public class Cluster
    {
        #region Constructors

        public Cluster(Color color, Cell[] cells)
        {
            this.Color = color;
            this.Cells = cells;
        }

        #endregion

        #region Properties

        public Color Color { get; }

        public Cell[] Cells { get; }

        #endregion
    }
}