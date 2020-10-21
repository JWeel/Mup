using Microsoft.Win32;
using Mup.Controls;
using Mup.Extensions;
using Mup.Helpers;
using Mup.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Mup
{
    public partial class Core : Window, INotifyPropertyChanged
    {
        #region Constants

        private const int WHITE_ARGB = unchecked((int) 0xFFFFFFFF);
        private const int BLACK_ARGB = unchecked((int) 0xFF000000);
        private const int TRANS_BLACK_ARGB = 0;
        private const int TRANS_WHITE_ARGB = unchecked((int) 0x00FFFFFF);
        private const int BORDER_ARGB = unchecked((int) 0xFF010101);
        private const int ROOT_ARGB = unchecked((int) 0xFF606060);
        private const int POP_ARGB = unchecked((int) 0xFFF5F5F5);

        private static readonly ISet<int> IGNORED_ARGB_SET = new HashSet<int>
        {
            WHITE_ARGB, BLACK_ARGB, TRANS_BLACK_ARGB, TRANS_WHITE_ARGB, BORDER_ARGB, ROOT_ARGB, POP_ARGB
        };

        private const string INITIAL_FILE_DIRECTORY = @"d:\Downloads\Hymi\Next";
        private const string QUICK_LOAD_PATH = @"d:\Downloads\Hymi\Next\a0.png";

        private const string REGEX_PATTERN_LEGAL_FILE_NAME = @"^(?!^(?:PRN|AUX|CLOCK\$|NUL|CON|COM\d|LPT\d)(?:\..+)?$)(?:\.*?(?!\.))[^\x00-\x1f\\?*:\"";|\/<>]+(?<![\s.])$";

        #endregion

        #region Constructors

        public Core()
        {
            this.ActiveIndex = new ClampedIndex();
            this.ActiveIndex.ValueChanged += this.HandleValueChanged;
            this.ImageHeaders = new ObservableCollection<ImageHeader>();
            this.ImageHeaders.CollectionChanged += this.HandleImageHeadersChanged;

            // initializes the child UI elements, they get initialized later automatically
            // but we need to do it now to access some elements in the ctor
            this.InitializeComponent();

            this.ImageHeaders.Add(this.ConfigureNewImageHeader());

            this.Stopwatch = new Stopwatch();
            this.MapImageZoomer.MapMousePointChanged += point =>
            {
                this.MapMemo = $"Pixel: {point.X:0}, {point.Y:0}";
                if (this.MapInfo == null)
                    return;

                var (x, y) = point;
                var color = this.MapInfo.Locate((int) x, (int) y);
                this.MapMemo += $"  Size: {this.MapInfo.SizeByColor[color]}";
                this.MapMemo += Environment.NewLine;
                this.MapMemo += color.Print();
            };
            this.MapImageZoomer.MouseDown += (o, e) =>
            {
                if ((e.ChangedButton != MouseButton.Middle) || (e.ButtonState != MouseButtonState.Pressed))
                    return;
                if (this.MapColorComparison == null)
                    return;

                var (x, y) = this.MapImageZoomer.MapMousePoint;
                var color = this.MapInfo.Locate((int) x, (int) y);

                if (!this.MapColorComparison.TryGetValue(color, out var sourceColors))
                    return;
                this.MapMemo = $"Colors in source: {sourceColors.Length}";
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

        public string InitialFileDirectory => INITIAL_FILE_DIRECTORY;

        protected bool MovingWindow { get; set; }

        protected Point PositionBeforeMoving { get; set; }

        protected Troolean QuickLoadEnabled { get; set; }

        public ObservableCollection<ImageHeader> ImageHeaders { get; set; }

        protected ClampedIndex ActiveIndex { get; set; }

        protected ImageHeader ActiveImageHeader => this.ImageHeaders[this.ActiveIndex];

        protected ImageModel ActiveImage => this.ActiveImageHeader.Model;

        protected byte[] ActiveImageData => this.ActiveImage?.Data;

        // private string _sourceFileDirectory;
        // protected string SourceFileDirectory => _sourceFileDirectory;

        // private string _sourcePath;
        // public string SourcePath
        // {
        //     get => _sourcePath;
        //     set
        //     {
        //         _sourceFileDirectory = Path.GetDirectoryName(value).CoalesceNullOrWhiteSpace(_sourceFileDirectory);
        //         _sourcePath = value;
        //         this.LastSourceFileName = value;
        //         this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SourcePath)));
        //     }
        // }

        // private string _lastSourceFileDirectory;
        // protected string LastSourceFileDirectory => _lastSourceFileDirectory;

        // private string _lastSourceFileName;
        // public string LastSourceFileName
        // {
        //     get => _lastSourceFileName;
        //     set
        //     {
        //         _lastSourceFileDirectory = Path.GetDirectoryName(value).CoalesceNullOrWhiteSpace(_lastSourceFileDirectory);
        //         _lastSourceFileName = Path.GetFileName(value);
        //         this.RefreshImageButton.IsEnabled = (value != null);
        //         this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.LastSourceFileName)));
        //     }
        // }

        // private string _targetFileName;
        // public string TargetFileName
        // {
        //     get => _targetFileName;
        //     set
        //     {
        //         _targetFileName = value;
        //         this.ConditionallyEnableSave();
        //     }
        // }

        // private FileState _fileState;
        // protected FileState FileState
        // {
        //     get => _fileState;
        //     set
        //     {
        //         _fileState = value;
        //         switch (value)
        //         {
        //             case FileState.SelectFile:
        //                 this.SelectFileButton.Show();
        //                 this.SourceFilePathTextBox.Collapse();
        //                 this.TargetFileNameWrapperGrid.Collapse();
        //                 this.OptionGrid.Collapse();
        //                 this.MapMemoLabel.Hide();
        //                 this.FlagGrid.Hide();
        //                 break;
        //             case FileState.SelectOption:
        //                 this.SelectFileButton.Collapse();
        //                 this.SourceFilePathTextBox.Show();
        //                 if (!this.AutoSaveFlag)
        //                     this.TargetFileNameWrapperGrid.Show();
        //                 this.OptionGrid.Show();
        //                 this.MapMemoLabel.Show();
        //                 this.FlagGrid.Show();
        //                 break;
        //         }
        //     }
        // }

        // private ImageState _imageState;
        // protected ImageState ImageState
        // {
        //     get => _imageState;
        //     set
        //     {
        //         _imageState = value;
        //         switch (value)
        //         {
        //             case ImageState.None:
        //             case ImageState.Loaded:
        //             case ImageState.Saved:
        //                 this.SaveImageButton.IsEnabled = false;
        //                 this.UndoImageButton.IsEnabled = false;
        //                 break;
        //             case ImageState.Pending:
        //                 this.ConditionallyEnableSave();
        //                 this.UndoImageButton.IsEnabled = (this.PreviousImageData != null);
        //                 break;
        //         }
        //     }
        // }

        protected ImageInfo MapInfo { get; set; }

        protected ImageInfo ClusterSourceMapInfo { get; set; }

        protected IDictionary<System.Drawing.Color, System.Drawing.Color[]> MapColorComparison { get; set; }

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

        // private byte[] _imageData;
        // protected byte[] ImageData2
        // {
        //     get => _imageData;
        //     set
        //     {
        //         this.PreviousImageData = _imageData;
        //         _imageData = value;
        //     }
        // }

        // protected byte[] PreviousImageData { get; set; }

        private byte[] _clusterSourceImageData;
        protected byte[] ClusterSourceImageData
        {
            get => _clusterSourceImageData;
            set
            {
                // maybe instead write one method which is "DetermineStateForEachButton"
                // then for each button wrire conditions that say if it is enabled/hidden/etc
                this.EnumerateAllChildren<Button>()
                    .Where(button => (button.Tag?.ToString() == "ClusterButton"))
                    .Each(button => button.IsEnabled = (value != null));
                this.RefineButton.IsEnabled = (this.ImageClusterGroups != null);
                _clusterSourceImageData = value;
            }
        }

        protected Cluster[][] PreviousImageClusterGroups { get; set; }

        private Cluster[][] _imageClusterGroups;
        protected Cluster[][] ImageClusterGroups
        {
            get => _imageClusterGroups;
            set
            {
                this.RefineButton.IsEnabled = (value != null);
                this.PreviousImageClusterGroups = _imageClusterGroups;
                _imageClusterGroups = value;
            }
        }

        protected ISet<System.Drawing.Color>[] ClusterBounds { get; set; }

        protected Stopwatch Stopwatch { get; }

        protected int MinBlobSize => this.MinBlobSizeSlideBar.Value;

        protected int MaxBlobSize => this.MaxBlobSizeSlideBar.Value;

        protected int IsleBlobSize => this.IsleBlobSizeSlideBar.Value;

        protected int AmountOfClusters => this.AmountOfClustersSlideBar.Value;

        protected int MaxIterations => this.MaxIterationsSlideBar.Value;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        protected ImageHeader ConfigureNewImageHeader()
        {
            var imageHeader = new ImageHeader();
            imageHeader.OnInit += this.HandleImageHeaderInit;
            imageHeader.OnUndo += this.HandleImageHeaderUndo;
            imageHeader.OnRedo += this.HandleImageHeaderRedo;
            imageHeader.OnClose += this.HandleImageHeaderClose;
            imageHeader.OnSelect += this.HandleImageHeaderSelect;
            imageHeader.FileNamePredicate = selectedFilePath =>
                this.ImageHeaders.All(header => !header.FilePath.Equals(selectedFilePath, StringComparison.InvariantCultureIgnoreCase));
            return imageHeader;
        }

        protected void HandleImageHeaderInit()
        {
            var imageHeader = this.ConfigureNewImageHeader();
            this.ImageHeaders.Add(imageHeader);
            var index = this.ImageHeaders.IndexOf(imageHeader);
            this.ActiveIndex.Value = index;
        }

        protected void HandleImageHeaderUndo() =>
            this.SetMapImage();

        protected void HandleImageHeaderRedo() =>
            this.SetMapImage();

        protected void HandleImageHeaderSelect(ImageHeader imageHeader)
        {
            var index = this.ImageHeaders.IndexOf(imageHeader);
            this.ActiveIndex.Value = index;
        }

        protected void HandleImageHeaderClose(ImageHeader imageHeader)
        {
            if (this.ImageHeaders.Count > 1)
                this.ImageHeaders.Remove(imageHeader);
        }

        protected void HandleImageHeadersChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            this.ActiveIndex.Max = this.ImageHeaders.Count(x => (x.Model != null)) - 1;
        }

        protected void HandleValueChanged()
        {
            this.ImageHeaders.Each(x => x.HeaderButton.IsEnabled = (x != this.ActiveImageHeader));
            this.SetMapImage();
        }

        protected void PressKey(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    this.Exit();
                    break;
                case Key.Right:
                    this.ActiveIndex++;
                    break;
                case Key.Left:
                    this.ActiveIndex--;
                    break;
                case Key.Z:
                    this.ActiveImageHeader.Undo(default, default);
                    break;
                case Key.X:
                    this.ActiveImageHeader.Redo(default, default);
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
            if (this.ImageHeaders.First().Model != null)
                return;

            var imageHeader = this.ConfigureNewImageHeader();
            imageHeader.Init(QUICK_LOAD_PATH);

            this.ImageHeaders[0] = imageHeader;
            this.ActiveIndex.Value = 0;
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
            // this.SourcePath = filePath;

            if (filePath.IsNullOrWhiteSpace())
                return;

            if (filePath == "unsaved") return;
            // this.ImageData = File.ReadAllBytes(filePath);
            // this.SetMapImage(ImageState.Loaded);
        }

        protected void SetMapImage()
        {
            var imageData = this.ActiveImage?.Data;
            if (imageData == null)
            {
                this.MapImage.Source = null;
                this.OptionGrid.Collapse();
                this.MapMemoLabel.Hide();
                this.FlagGrid.Hide();
                return;
            }

            this.OptionGrid.Show();
            this.MapMemoLabel.Show();
            this.FlagGrid.Show();

            this.MapImage.Source = imageData.ToBitmapImage();
            Task.Run(async () =>
            {
                var mupper = new Mupper();
                this.MapInfo = await mupper.InfoAsync(imageData);
                this.MapMemo = $"Colors: {this.MapInfo.NonEdgeColorSet.Count}";

                if (this.ClusterSourceMapInfo == null)
                    return;

                this.MapColorComparison = this.MapInfo.Pixels
                    .WithIndex()
                    .Select(x => (Current: x.Value, Source: this.ClusterSourceMapInfo.Pixels[x.Index]))
                    .GroupBy(x => x.Current)
                    .ToDictionary(group => group.Key, group => group
                        .Select(x => x.Source)
                        .Distinct()
                        .ToArray());
            });
        }

        protected void CenterImage(object sender, RoutedEventArgs e)
        {
            this.MapImageZoomer.Reset();
        }

        protected void RefreshImage(object sender, RoutedEventArgs e)
        {
            // var lastSourceFilePath = Path.Combine(this.LastSourceFileDirectory, this.LastSourceFileName);
            // this.SelectFile(lastSourceFilePath);
        }

        // protected void UnloadImage(object sender, RoutedEventArgs e)
        // {
        //     this.MapImage.Source = null;
        //     this.SelectFile(null);
        //     this.FileState = FileState.SelectFile;
        //     this.ImageState = ImageState.None;
        // }

        protected void SaveImage(object sender, RoutedEventArgs e) =>
            this.SaveImage();

        protected void SaveImage()
        {
            // if (this.ImageData == null)
            //     return;

            // if (!Path.HasExtension(targetFileName))
            //     targetFileName += ".png";

            // var targetFilePath = Path.Combine(this.SourceFileDirectory, targetFileName);
            // if (File.Exists(targetFilePath))
            // {
            //     this.AutoSaveFlag = false;
            //     var confirmation = MessageBox.Show("Overwrite file?", "File exists", MessageBoxButton.YesNo);
            //     if (confirmation != MessageBoxResult.Yes)
            //         return;
            // }

            // this.PreviousImageData = null;
            // // this.ImageData.SaveToImage(targetFilePath);
            // this.SourcePath = targetFilePath;
            // this.ImageState = ImageState.Saved;
        }

        protected void SaveAsImage(object sender, RoutedEventArgs e)
        {
        }

        protected void UndoImage(object sender, RoutedEventArgs e)
        {
            // if (this.PreviousImageData == null)
            //     return;
            // // this.ImageData = this.PreviousImageData;
            // this.ImageClusterGroups = this.PreviousImageClusterGroups;
            // this.PreviousImageData = null;
            // this.PreviousImageClusterGroups = null;
            // this.AutoSaveFlag = false;
            // this.AfterMupperImaging();
            // this.UndoImageButton.IsEnabled = false;
        }

        protected async void LogImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;

            var targetFileName = this.GetTimeStampedFileName(".log");
            var targetFilePath = Path.Combine(this.ActiveImageHeader.InitialFileDirectory, targetFileName);
            using var scope = this.ScopedMupperLoggingOperation();
            await scope.Value.LogAsync(this.ActiveImageData, targetFilePath);
        }

        protected async void RepaintImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await scope.Value.RepaintAsync(this.ActiveImageData, this.ContiguousFlag);
            this.ActiveImage.Advance(bitmap.ToPNG());
        }

        protected async void MergeImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await scope.Value.MergeAsync(this.ActiveImageData, this.ContiguousFlag, this.MinBlobSize, this.MaxBlobSize, this.IsleBlobSize);
            this.ActiveImage.Advance(bitmap.ToPNG());
        }

        protected async void BorderImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await scope.Value.BorderAsync(this.ActiveImageData, BORDER_ARGB);
            this.ActiveImage.Advance(bitmap.ToPNG());
        }

        protected async void ExtractImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await scope.Value.ExtractAsync(this.ActiveImageData);
            this.ActiveImage.Advance(bitmap.ToPNG());
        }

        protected async void SplitImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await scope.Value.SplitAsync(this.ActiveImageData, this.ContiguousFlag, this.MinBlobSize, this.MaxBlobSize);
            this.ActiveImage.Advance(bitmap.ToPNG());
        }

        protected async void ColonyImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await scope.Value.ColonyAsync(this.ActiveImageData, this.MaxBlobSize, this.IsleBlobSize);
            this.ActiveImage.Advance(bitmap.ToPNG());
        }

        protected async void CheckImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await scope.Value.CheckAsync(this.ActiveImageData, this.MinBlobSize, this.MaxBlobSize, this.IsleBlobSize);
            this.ActiveImage.Advance(bitmap.ToPNG());
        }

        protected async void EdgeImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            using var bitmap = await scope.Value.EdgeAsync(this.ActiveImageData, this.ContiguousFlag);
            this.ActiveImage.Advance(bitmap.ToPNG());
        }

        protected void SourceImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            this.ImageClusterGroups = null;
            this.ClusterSourceImageData = this.ActiveImageData;
            this.ClusterSourceMapInfo = this.MapInfo;
        }

        protected async void BoundImage(object sender, RoutedEventArgs e)
        {
            if (this.ClusterSourceImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            // this.ClusterBounds = await scope.Value.BoundAsync(this.ImageData, this.ClusterSourceImageData, this.ClusterBounds, IGNORED_ARGB_SET);
        }

        protected void UnboundImage(object sender, RoutedEventArgs e)
        {
            this.ClusterBounds = null;
        }

        protected async void PopImage(object sender, RoutedEventArgs e)
        {
            using var scope = this.ScopedMupperImagingOperation();
            // var bitmap = await scope.Value.PopAsync(this.ImageData, this.ClusterSourceImageData, this.ClusterBounds, POP_ARGB, ROOT_ARGB, IGNORED_ARGB_SET);
            // this.ImageData = bitmap.ToPNG();
        }

        protected async void ClusterImage(object sender, RoutedEventArgs e)
        {
            if (this.ClusterSourceImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            // var (bitmap, clusters) = await scope.Value.ClusterAsync(this.ImageData, this.ClusterSourceImageData, this.ClusterBounds, this.AmountOfClusters, ROOT_ARGB, IGNORED_ARGB_SET);
            // this.ImageData = bitmap.ToPNG();
            // if (this.ClusterBounds == null)
            //     this.ClusterBounds = await scope.Value.BoundAsync(this.ImageData, this.ClusterSourceImageData, this.ClusterBounds, IGNORED_ARGB_SET);
            // this.ImageClusterGroups = clusters.IntoArray();
        }

        protected async void RefineImage(object sender, RoutedEventArgs e)
        {
            if (this.ClusterSourceImageData == null)
                return;
            if (this.ImageClusterGroups?.FirstOrDefault().OrDefault(x => x.Length) % this.AmountOfClusters != 0)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            // var (bitmap, clusterGroups) = await scope.Value.RefineAsync(this.ClusterSourceImageData, this.ImageData, this.ImageClusterGroups, this.AmountOfClusters, this.MaxIterations, ROOT_ARGB, POP_ARGB);
            // this.ImageData = bitmap.ToPNG();
            // this.ImageClusterGroups = clusterGroups;
        }

        protected async void AllocateImage(object sender, RoutedEventArgs e)
        {
            if (this.ClusterSourceImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            // var bitmap = await scope.Value.AllocateAsync(this.ImageData, POP_ARGB, this.AmountOfClusters, this.MaxIterations);
            // this.ImageData = bitmap.ToPNG();
        }

        protected void ColorImage(object sender, RoutedEventArgs e)
        {
            var color = Generate.MupColor(this.MapInfo.NonEdgeColorSet);
            this.MapMemo = color.Print();
            Clipboard.SetText(color.PrintHex());
        }

        protected Scope<Mupper> ScopedMupperLoggingOperation() =>
            new Scope<Mupper>(new Mupper(), this.BeforeMupper, this.AfterMupper);

        protected Scope<Mupper> ScopedMupperImagingOperation() =>
            new Scope<Mupper>(new Mupper(), this.BeforeMupper, this.AfterMupperImaging);

        protected Cursor PreviousCursor { get; set; }
        protected void BeforeMupper()
        {
            this.SetOptionsEnabledState(state: false);
            this.UndoImageButton.IsEnabled = false;
            this.PreviousCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
            this.Stopwatch.Restart();
        }

        protected void AfterMupper()
        {
            this.Stopwatch.Stop();
            Console.WriteLine($"Mupper operation took: {this.Stopwatch.Elapsed}");
            this.SetOptionsEnabledState(state: true);
            Mouse.OverrideCursor = this.PreviousCursor;
            this.PreviousCursor = null;
        }

        protected void AfterMupperImaging()
        {
            this.AfterMupper();
            this.SetMapImage();
            // this.SourcePath = "unsaved";
            // if (this.AutoSaveFlag)
            //     this.SaveImage();
        }

        protected void SetOptionsEnabledState(bool state)
        {
            this.QuickLoadEnabled = state;
            // this.MupperGrid.EnumerateAllChildren<Button>()
            //     .Where(button => (button.Tag?.ToString() != "ClusterButton"))
            //     .Each(button => button.IsEnabled = state);
        }

        protected string GetTimeStampedFileName(string extension) =>
            DateTime.Now.Ticks.ToString() + extension;

        protected void ConditionallyEnableSave()
        {
            // var isEmptyName = this.TargetFileName.IsNullOrWhiteSpace();
            // var isIllegalFileName = (!isEmptyName && !Regex.IsMatch(this.TargetFileName, REGEX_PATTERN_LEGAL_FILE_NAME));
            // if (isIllegalFileName)
            //     this.TargetFileNameTextBox.Background = Core.TextInputErrorBrush;
            // else
            //     this.TargetFileNameTextBox.Background = Core.TextInputBackgroundBrush;
            // this.SaveImageButton.IsEnabled = (!isEmptyName && !isIllegalFileName);
        }

        protected void HandleControlVisibility()
        {

        }

        protected void Exit(object sender, RoutedEventArgs e) =>
            this.Exit();

        protected void Exit() =>
            Environment.Exit(0);

        #endregion
    }
}