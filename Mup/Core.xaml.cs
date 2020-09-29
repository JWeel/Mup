using Microsoft.Win32;
using Mup.Extensions;
using Mup.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
            this.ImageState = ImageState.None;

            this.MapImageZoomer.MapMousePointChanged += point =>
                this.MapMemo = $"Pixel Coordinate: {point.X:0}, {point.Y:0}";
            this.MapImageZoomer.MouseDown += (o, e) =>
            {
                if ((e.ChangedButton != MouseButton.Middle) || (e.ButtonState != MouseButtonState.Pressed))
                    return;
                if (this.MapInfo == null)
                    return;

                var (x, y) = this.MapImageZoomer.MapMousePoint;
                var color = this.MapInfo.Locate((int) x, (int) y);
                this.MapMemo = $"R:{color.R} G:{color.G} B:{color.B}";
            };
            this.SidePanel.MouseEnter += (o, e) =>
            {
                if (this.MapInfo == null)
                    return;

                this.MapMemo = $"Colors: {this.MapInfo.NonEdgeColorSet.Count}";
            };
        }

        #endregion

        #region Properties

        protected static SolidColorBrush TextInputBackgroundBrush { get; } =
            App.Current.Resources[nameof(TextInputBackgroundBrush)] as SolidColorBrush;

        protected static SolidColorBrush TextInputErrorBrush { get; } =
            App.Current.Resources[nameof(TextInputErrorBrush)] as SolidColorBrush;

        protected Troolean QuickLoadEnabled { get; set; }

        private string _sourceFileName;
        protected string SourceFileName => _sourceFileName;

        private string _sourceFileDirectory;
        protected string SourceFileDirectory => _sourceFileDirectory;

        private string _sourcePath;
        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                _sourceFileName = Path.GetFileName(value);
                _sourceFileDirectory = Path.GetDirectoryName(value).CoalesceNullOrWhiteSpace(_sourceFileDirectory);
                _sourcePath = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SourcePath)));
            }
        }

        private const string REGEX_PATTERN_LEGAL_FILE_NAME = @"^(?!^(?:PRN|AUX|CLOCK\$|NUL|CON|COM\d|LPT\d)(?:\..+)?$)(?:\.*?(?!\.))[^\x00-\x1f\\?*:\"";|\/<>]+(?<![\s.])$";
        private string _targetFileName;
        public string TargetFileName
        {
            get => _targetFileName;
            set
            {
                var isEmptyName = value.IsNullOrWhiteSpace();
                var isIllegalFileName = (!isEmptyName && !Regex.IsMatch(value, REGEX_PATTERN_LEGAL_FILE_NAME));
                if (isEmptyName || isIllegalFileName)
                    this.SaveImageButton.IsEnabled = false;
                if (isIllegalFileName)
                    this.TargetFileNameTextBox.Background = Core.TextInputErrorBrush;
                else
                    this.TargetFileNameTextBox.Background = Core.TextInputBackgroundBrush;
                if (!isEmptyName && !isIllegalFileName)
                    this.SaveImageButton.IsEnabled = true;

                _targetFileName = value;
            }
        }

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
                        this.SourceFilePathTextBox.Collapse();
                        this.TargetFileNameWrapperGrid.Collapse();
                        this.OptionGrid.Collapse();
                        this.MapMemoLabel.Hide();
                        this.FlagGrid.Hide();
                        break;
                    case FileState.SelectOption:
                        this.SelectFileButton.Collapse();
                        this.SourceFilePathTextBox.Show();
                        if (!this.AutoSaveFlag)
                            this.TargetFileNameWrapperGrid.Show();
                        this.OptionGrid.Show();
                        this.MapMemoLabel.Show();
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

        protected ImageInfo MapInfo { get; set; }

        private string _mapMemo;
        public string MapMemo
        {
            get => _mapMemo;
            set
            {
                _mapMemo = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.MapMemo)));
            }
        }

        public bool ContiguousFlag { get; set; }

        private bool _autoSaveFlag;
        public bool AutoSaveFlag
        {
            get => _autoSaveFlag;
            set
            {
                if (value)
                {
                    this.TargetFileNameWrapperGrid.Collapse();
                    this.SaveImageButton.Collapse();
                }
                else
                {
                    this.TargetFileNameWrapperGrid.Show();
                    if (this.FileState == FileState.SelectOption)
                        this.SaveImageButton.Show();
                }

                _autoSaveFlag = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.AutoSaveFlag)));
            }
        }

        protected byte[] ImageData { get; set; }

        protected int MinBlobSize => (int) this.MinBlobSizeSlider.Value;

        protected int MaxBlobSize => (int) this.MaxBlobSizeSlider.Value;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        protected void PressKey(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    this.Exit();
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
            this.MapInfo = null;
            this.MapMemo = string.Empty;
            this.SourcePath = filePath;

            if (filePath.IsNullOrWhiteSpace())
                return;

            this.ImageData = File.ReadAllBytes(filePath);
            this.SetMapImage(ImageState.Loaded);
        }

        protected void SetMapImage(ImageState imageState)
        {
            this.MapImage.Source = this.ImageData.ToBitmapImage();
            this.FileState = FileState.SelectOption;
            this.ImageState = imageState;
            Task.Run(async () =>
            {
                var mupper = new Mupper();
                this.MapInfo = await mupper.InfoAsync(this.ImageData);
            });
        }

        protected void CenterImage(object sender, RoutedEventArgs e)
        {
            this.MapImageZoomer.Reset();
        }

        protected void UnloadImage(object sender, RoutedEventArgs e)
        {
            this.MapImage.Source = null;
            this.SelectFile(null);
            this.FileState = FileState.SelectFile;
            this.ImageState = ImageState.None;
        }

        protected void SaveImage(object sender, RoutedEventArgs e) =>
            this.SaveImage();

        protected void SaveImage()
        {
            if (this.ImageData == null)
                return;

            var targetFileName = this.AutoSaveFlag ? this.GetTimeStampedFileName(".png") : this.TargetFileName;
            if (targetFileName.IsNullOrWhiteSpace())
                return;

            var targetFilePath = Path.Combine(this.SourceFileDirectory, targetFileName);
            if (File.Exists(targetFilePath))
            {
                this.AutoSaveFlag = false;
                var confirmation = MessageBox.Show("Overwrite file?", "File exists", MessageBoxButton.YesNo);
                if (confirmation != MessageBoxResult.Yes)
                    return;
            }

            this.ImageData.SaveToImage(targetFilePath);
            this.SourcePath = targetFilePath;
            this.ImageState = ImageState.Saved;
        }

        protected async void LogImage(object sender, RoutedEventArgs e)
        {
            var targetFileName = this.GetTimeStampedFileName(".log");
            var targetFilePath = Path.Combine(this.SourceFileDirectory, targetFileName);
            var mupper = new Mupper();
            using var scope = this.ScopedMupperLoggingOperation();
            await mupper.LogAsync(this.ImageData, targetFilePath);
        }

        protected async void RepaintImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await mupper.RepaintAsync(this.ImageData, this.ContiguousFlag);
            this.ImageData = bitmap.ToPNG();
        }

        protected async void MergeImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await mupper.MergeAsync(this.ImageData, this.ContiguousFlag, this.MinBlobSize, this.MaxBlobSize);
            this.ImageData = bitmap.ToPNG();
        }

        private const int BORDER_ARGB = unchecked((int) 0xFF010101);
        protected async void BorderImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await mupper.BorderAsync(this.ImageData, BORDER_ARGB);
            this.ImageData = bitmap.ToPNG();
        }

        protected async void ExtractImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await mupper.ExtractAsync(this.ImageData);
            this.ImageData = bitmap.ToPNG();
        }

        protected async void SeparateImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await mupper.SeparateAsync(this.ImageData, this.ContiguousFlag, this.MinBlobSize, this.MaxBlobSize);
            this.ImageData = bitmap.ToPNG();
        }

        protected async void ColonyImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await mupper.ColonyAsync(this.ImageData, this.MinBlobSize, this.MaxBlobSize);
            this.ImageData = bitmap.ToPNG();
        }

        protected async void CheckImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await mupper.CheckAsync(this.ImageData, this.ImageData);
            this.ImageData = bitmap.ToPNG();
        }

        protected Scope ScopedMupperLoggingOperation() =>
            new Scope(this.BeforeMupper, this.AfterMupperLogging);

        protected Scope ScopedMupperImagingOperation() =>
            new Scope(this.BeforeMupper, this.AfterMupperImaging);

        protected void BeforeMupper()
        {
            this.SetOptionsEnabledState(state: false);
        }

        protected void AfterMupperLogging()
        {
            this.SetOptionsEnabledState(state: true);
        }

        protected void AfterMupperImaging()
        {
            this.SetMapImage(ImageState.Pending);
            this.SetOptionsEnabledState(state: true);
            this.SourcePath = "in memory";
            if (this.AutoSaveFlag)
                this.SaveImage(); // autosave should use timestamp
        }

        protected void SetOptionsEnabledState(bool state)
        {
            this.QuickLoadEnabled = state;
            this.OptionGrid.EnumerateAllChildren<Button>()
                .Each(button => button.IsEnabled = state);
        }

        protected string GetTimeStampedFileName(string extension) =>
            DateTime.Now.Ticks.ToString() + extension;

        protected void Exit(object sender, RoutedEventArgs e) =>
            this.Exit();

        protected void Exit() =>
            Environment.Exit(0);

        #endregion
    }
}
