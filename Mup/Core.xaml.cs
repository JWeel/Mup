using Microsoft.Win32;
using Mup.Extensions;
using Mup.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mup
{
    public partial class Core : Window, INotifyPropertyChanged
    {
        #region Constructors

        public Core()
        {
            // initializes the child UI elements, they get initialized later automatically
            // but we need to do it now to access some elements in the ctor
            this.InitializeComponent();

            this.FileState = FileState.SelectFile;
            this.DataContext = this;
            this.ContiguousFlagCheckBox.DataContext = this;

            this.MapImageZoomer.MapMousePointChanged += point =>
                this.MapInfo = $"Pixel Coordinate: {point.X:0}, {point.Y:0}";
        }

        #endregion

        #region Properties

        protected Troolean QuickLoadEnabled { get; set; }

        protected string SelectedFileName { get; set; }

        protected string SelectedFileDirectory { get; set; }

        protected bool MovingWindow { get; set; }

        protected Point PositionBeforeMoving { get; set; }

        private FileState _fileState;
        protected FileState FileState
        {
            get => _fileState;
            set
            {
                _fileState = value;
                switch (value)
                {
                    case FileState.SelectFile:
                        this.SelectFileButton.Show();
                        this.FileNameTextBox.Collapse();
                        this.OptionGrid.Collapse();
                        this.MapInfoLabel.Hide();
                        this.FlagGrid.Hide();
                        break;
                    case FileState.SelectOption:
                        this.SelectFileButton.Collapse();
                        this.FileNameTextBox.Show();
                        this.OptionGrid.Show();
                        this.MapInfoLabel.Show();
                        this.FlagGrid.Show();
                        break;
                }
            }
        }

        private ImageState _imageState;
        protected ImageState ImageState
        {
            get => _imageState;
            set
            {
                _imageState = value;
                switch (value)
                {
                    case ImageState.None:
                    case ImageState.Loaded:
                    case ImageState.Saved:
                        this.SaveImageButton.Collapse();
                        break;
                    case ImageState.Pending:
                        this.SaveImageButton.Show();
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

        public bool ContiguousFlag { get; set; }

        private bool _autoSaveFlag;
        public bool AutoSaveFlag
        {
            get => _autoSaveFlag;
            set
            {
                _autoSaveFlag = value;
            }
        }

        protected byte[] ImageData { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        protected void PressKey(object sender, KeyEventArgs e)
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
            if ((e.ChangedButton != MouseButton.Right) || (e.RightButton != MouseButtonState.Pressed))
                return;
            this.PositionBeforeMoving = e.GetPosition(this);
            this.MovingWindow = true;
        }

        protected void StopDragWindow(object sender, MouseButtonEventArgs e)
        {
            if ((e.ChangedButton != MouseButton.Right) || (e.RightButton != MouseButtonState.Released))
                return;
            this.MovingWindow = false;
        }

        protected void DragWindow(object sender, MouseEventArgs e)
        {
            if (!this.MovingWindow)
                return;
            var endPosition = e.GetPosition(this);
            var vector = endPosition - this.PositionBeforeMoving;
            this.Left += vector.X;
            this.Top += vector.Y;
        }

        protected void QuickLoad(object sender, RoutedEventArgs e)
        {
            if (!this.QuickLoadEnabled)
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

        protected void SelectFile(string filePath)
        {
            // Path methods are save to use with null/empty arguments
            this.SelectedFileName = Path.GetFileName(filePath);
            this.SelectedFileDirectory = Path.GetDirectoryName(filePath);
            this.FileNameTextBox.Text = filePath;
            this.MapInfo = string.Empty;

            if (filePath.IsNullOrWhiteSpace())
                return;

            var imageData = File.ReadAllBytes(filePath);
            this.SetMapImage(imageData, ImageState.Loaded);
        }

        protected void SetMapImage(byte[] imageData, ImageState imageState)
        {
            this.ImageData = imageData;
            this.MapImage.Source = imageData.ToBitmapImage();
            this.FileState = FileState.SelectOption;
            this.ImageState = imageState;

            if (this.AutoSaveFlag)
                this.SaveImage();
        }

        protected void CenterImage(object sender, RoutedEventArgs e)
        {
            this.MapImageZoomer.Reset();
        }

        protected void UnloadImage(object sender, RoutedEventArgs e)
        {
            this.MapImage.Source = null;
            this.MapImageZoomer.Reset();
            this.SelectFile(null);
            this.FileState = FileState.SelectFile;
            this.ImageState = ImageState.None;
        }

        protected void SaveImage(object sender, RoutedEventArgs e) =>
            this.SaveImage();

        protected void SaveImage()
        {
            if ((this.ImageData == null)
            || this.SelectedFileName.IsNullOrWhiteSpace()
            || this.SelectedFileDirectory.IsNullOrWhiteSpace())
                return;

            this.ImageState = ImageState.Saved;
            return; // avoid accidental save. need to change selectedfilename or create a second textbox for target

            // using var stream = new MemoryStream(this.ImageData);
            // // TODO or use Image.FromStream(stream);
            // var bitmap = new System.Drawing.Bitmap(stream);
            // var filePath = Path.Combine(this.SelectedFileDirectory, this.SelectedFileName);
            // bitmap.Save(filePath);

            // this.ImageState = ImageState.Saved;
        }

        protected async void LogImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            var sourceFilePath = Path.Combine(this.SelectedFileDirectory, this.SelectedFileName);
            var targetFileName = Path.GetFileNameWithoutExtension(this.SelectedFileName) + ".log";
            var targetFilePath = Path.Combine(this.SelectedFileDirectory, targetFileName);
            using (new Scope(this.DisableButtons, this.EnableButtons))
                await Task.Run(() => mupper.Log(sourceFilePath, targetFilePath));
        }

        protected async void RepaintImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            var sourceFilePath = Path.Combine(this.SelectedFileDirectory, this.SelectedFileName);
            var targetFileName = Path.GetFileNameWithoutExtension(this.SelectedFileName) + "_p.png";
            var targetFilePath = Path.Combine(this.SelectedFileDirectory, targetFileName);
            using (new Scope(this.DisableButtons, this.EnableButtons))
                await Task.Run(() => mupper.Repaint(sourceFilePath, targetFilePath, this.ContiguousFlag));
            this.SelectFile(targetFilePath);
        }

        protected async void IslandsImage(object sender, RoutedEventArgs e)
        {
            await Task.FromResult(0);
        }

        protected async void MergeImage(object sender, RoutedEventArgs e)
        {
            await Task.FromResult(0);
        }

        private const int BORDER_ARGB = unchecked((int) 0xFF010101);
        protected async void BorderImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            var sourceFilePath = Path.Combine(this.SelectedFileDirectory, this.SelectedFileName);
            var targetFileName = Path.GetFileNameWithoutExtension(this.SelectedFileName) + "_b.png";
            var targetFilePath = Path.Combine(this.SelectedFileDirectory, targetFileName);
            using (new Scope(this.DisableButtons, this.EnableButtons))
                await Task.Run(() => mupper.Border(sourceFilePath, targetFilePath, BORDER_ARGB));
            this.SelectFile(targetFilePath);
        }

        protected async void ExtractImage(object sender, RoutedEventArgs e)
        {
            var sourceFilePath = Path.Combine(this.SelectedFileDirectory, this.SelectedFileName);
            // var targetFileName = Path.GetFileNameWithoutExtension(this.SelectedFileName) + "_x.png";
            // var targetFilePath = Path.Combine(this.SelectedFileDirectory, targetFileName);
            var mupper = new Mupper();
            using var scope = new Scope(this.DisableButtons, this.EnableButtons);
            using var bitmap = await mupper.ExtractAsync(this.ImageData);
            var imageData = bitmap.ToPNG();
            this.SetMapImage(imageData, ImageState.Pending);
        }

        protected void DisableButtons()
        {
            this.QuickLoadEnabled = false;
            this.EnumerateAllChildren<Button>()
                .Each(button => button.IsEnabled = false);
        }

        protected void EnableButtons()
        {
            this.QuickLoadEnabled = true;
            this.EnumerateAllChildren<Button>()
                .Each(button => button.IsEnabled = true);
        }

        protected void Exit(object sender, RoutedEventArgs e) =>
            Environment.Exit(0);

        #endregion
    }
}
