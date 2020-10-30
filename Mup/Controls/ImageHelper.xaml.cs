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

        public ImageInfo Info { get; protected set; }

        protected ImageModel Model { get; set; }

        public byte[] Data => this.Model?.Data;

        #endregion

        #region Methods

        public void Set(byte[] data, ImageInfo info)
        {
            this.Model = new ImageModel(data);
            this.Info = info;
        }

        #endregion
    }
}