using Mup.Models;
using System.Windows.Controls;

namespace Mup.Controls
{
    public partial class ImageHelper : UserControl
    {
        #region Constructors

        public ImageHelper()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        #endregion

        #region Properties

        public string Symbol { get; set; }

        public ImageModel Model { get; set; }

        #endregion
    }
}