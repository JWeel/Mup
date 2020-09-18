using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Mup.Extensions;

namespace Mup
{
    public partial class Core : Window, INotifyPropertyChanged
    {
        #region Constructors

        public Core()
        {
            this.InitializeComponent();

            this.State = MupState.SelectFile;
            this.DataContext = this;

            this.MapImageZoomer.MapMousePointChanged += point =>
                this.MapInfo = $"Pixel Coordinate: {point.X:0}, {point.Y:0}"; ;
        }

        #endregion

        #region Properties

        protected string SelectedFileName { get; set; }

        protected bool MovingWindow { get; set; }

        protected Point PositionBeforeMoving { get; set; }

        private MupState _state;
        protected MupState State
        {
            get => _state;
            set
            {
                _state = value;
                switch (value)
                {
                    case MupState.SelectFile:
                        this.SelectFileButton.Show();
                        this.FileNameTextBox.Collapse();
                        this.OptionGrid.Collapse();
                        this.MapInfoLabel.Hide();
                        break;
                    case MupState.SelectOption:
                        this.SelectFileButton.Collapse();
                        this.FileNameTextBox.Show();
                        this.OptionGrid.Show();
                        this.MapInfoLabel.Show();
                        break;
                }
            }
        }

        private string _mapInfo;
        public string MapInfo
        {
            get => _mapInfo;
            set
            {
                _mapInfo = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.MapInfo)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        protected void Pressed(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Environment.Exit(0);
                    break;
            }
        }

        protected void InitDragWindow(object sender, MouseButtonEventArgs e)
        {
            if ((e.ChangedButton == MouseButton.Right) && (e.RightButton == MouseButtonState.Pressed))
            {
                this.PositionBeforeMoving = e.GetPosition(this);
                this.MovingWindow = true;
            }
        }

        protected void StopDragWindow(object sender, MouseButtonEventArgs e)
        {
            if ((e.ChangedButton == MouseButton.Right) && (e.RightButton == MouseButtonState.Released))
            {
                this.MovingWindow = false;
            }
        }

        protected void DragWindow(object sender, MouseEventArgs e)
        {
            if (this.MovingWindow)
            {
                var endPosition = e.GetPosition(this);
                var vector = endPosition - this.PositionBeforeMoving;
                this.Left += vector.X;
                this.Top += vector.Y;
            }
        }

        protected void QuickLoad(object sender, RoutedEventArgs e)
        {
            if (!this.SelectFileButton.IsEnabled)
                return;

            var fileName = @"d:\Downloads\Hymi\Next\a0.png";
            this.SelectFile(fileName);
        }

        protected void SelectFile(object sender, RoutedEventArgs a)
        {
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = @"d:\Downloads\Hymi\Next";
            dialog.DefaultExt = ".png";
            dialog.Filter = "PNG Files (*.png)|*.png|All files (*.*)|*.*";

            if (dialog.ShowDialog() ?? false)
                this.SelectFile(dialog.FileName);
        }

        protected void SelectFile(string fileName)
        {
            this.SelectedFileName = fileName;
            this.FileNameTextBox.Text = fileName;
            this.MapInfo = string.Empty;

            if (fileName.IsNullOrWhiteSpace())
                return;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = new Uri(fileName, UriKind.Absolute);
            image.EndInit();

            this.MapImage.Source = image;
            this.State = MupState.SelectOption;
        }

        protected void ResetImage(object sender, RoutedEventArgs e)
        {
            this.MapImageZoomer.Reset();
        }

        protected void ClearImage(object sender, RoutedEventArgs e)
        {
            this.MapImage.Source = null;
            this.MapImageZoomer.Reset();
            this.SelectFile(null);
            this.State = MupState.SelectFile;
        }

        protected void Exit(object sender, RoutedEventArgs e) =>
            Environment.Exit(0);

        #endregion
    }
}
