using Mup.External;
using System.Collections.Generic;
using System.Drawing;

namespace Mup.Models
{
    public class Cell
    {
        #region Constructors

        public Cell(Color color, Vector center)
        {
            this.Color = color;
            this.Center = center;
            this.Children = new List<Cell>();
        }

        #endregion

        #region Properties

        public Color Color { get; }

        public Vector Center { get; }

        private Cell _parent;
        public Cell Parent
        {
            get => _parent;
            set
            {
                if ((value == null) && (_parent != null))
                    _parent.Children.Remove(this);
                if (value != null)
                    value.Children.Add(this);
                _parent = value;
            }
        }

        public List<Cell> Children { get; set; }

        #endregion
    }
}