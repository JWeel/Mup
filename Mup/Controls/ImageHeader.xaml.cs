using Microsoft.Win32;
using Mup.Extensions;
using Mup.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Mup.Controls
{
    public partial class ImageHeader : UserControl
    {
        #region Constants

        private const string FILE_EXTENSION_PNG = ".png";
        private const string FILE_FILTER_PNG = "PNG Files (*.png)|*.png|All files (*.*)|*.*";

        #endregion

        #region Constructor

        public ImageHeader()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        #endregion

        #region Properties

        // value gets set by binding so simple get/set is sufficient
        public string InitialFileDirectory { get; set; }

        // value gets set in code so need to use DependencyObject get/set
        public ImageModel Model
        {
            get => (ImageModel) this.GetValue(ImageHeader.ModelProperty);
            set => this.SetValue(ImageHeader.ModelProperty, value);
        }

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(nameof(ImageHeader.Model), typeof(ImageModel), typeof(ImageHeader), new PropertyMetadata(null));

        public static readonly DependencyProperty InitialFileDirectoryProperty =
            DependencyProperty.Register(nameof(ImageHeader.InitialFileDirectory), typeof(string), typeof(ImageHeader), new PropertyMetadata(string.Empty));

        public Func<string, bool> FileNamePredicate { get; set; }

        public event Action OnInit;

        public event Action<ImageHeader> OnClose;

        #endregion

        #region Methods

        public void Init(object sender, RoutedEventArgs args)
        {
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = this.InitialFileDirectory;
            dialog.DefaultExt = FILE_EXTENSION_PNG;
            dialog.Filter = FILE_FILTER_PNG;
            if (dialog.ShowDialog().Not())
                return;
            if (!this.FileNamePredicate.InvokeOrFalseIfNull(dialog.FileName))
                return;

            this.Model = new ImageModel();
            this.Model.Load(dialog.FileName);

            this.InitButton.Collapse();
            this.HeaderGrid.Show();

            this.OnInit?.Invoke();
        }

        public void Close(object sender, RoutedEventArgs args)
        {
            this.HeaderGrid.Collapse();
            this.InitButton.Show();

            this.OnClose?.Invoke(this);
        }

        #endregion
    }
}