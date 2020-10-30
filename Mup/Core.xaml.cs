using Microsoft.Win32;
using Mup.Controls;
using Mup.Extensions;
using Mup.Helpers;
using Mup.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    public partial class Core : Window
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

        private const int MAXIMUM_IMAGE_HEADER_COUNT = 9;

        #endregion

        #region Constructors

        public Core()
        {
            this.ImageHeaders = new Timeline<ImageHeader>();
            this.ImageHeaders.OnChangedCurrent += this.HandleChangedImageHeader;
            this.ImageHeaders.CollectionChanged += this.HandleChangedImageHeaderCollection;

            // initializes the child UI elements, they get initialized later automatically?
            // but we need to do it now to access some elements in the ctor
            this.InitializeComponent();

            this.Stopwatch = new Stopwatch();
            this.MapImageZoomer.MapMousePointChanged += point =>
            {
                this.MapMemo = $"Pixel: {point.X:0}, {point.Y:0}";
                if (this.MapInfo == null)
                    return;

                var (x, y) = point;
                var color = this.MapInfo.Locate((int) x, (int) y);
                if (color == default)
                    return;
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

        public Timeline<ImageHeader> ImageHeaders { get; set; }

        protected ImageHeader ActiveImageHeader => this.ImageHeaders.Current;

        protected ImageModel ActiveImage => this.ActiveImageHeader?.Model;

        protected byte[] ActiveImageData => this.ActiveImage?.Data;

        protected ImageInfo MapInfo { get; set; }

        protected ImageInfo ClusterSourceMapInfo { get; set; }

        protected IDictionary<System.Drawing.Color, System.Drawing.Color[]> MapColorComparison { get; set; }

        public string MapMemo
        {
            get => (string) this.GetValue(Core.MapMemoProperty);
            set => this.SetValue(Core.MapMemoProperty, value);
        }

        public static readonly DependencyProperty MapMemoProperty = typeof(Core).Register(nameof(Core.MapMemo), string.Empty);

        public bool ContiguousFlag { get; set; }

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

        #endregion

        #region Methods

        protected ImageHeader ConfigureNewImageHeader(string filePath)
        {
            var imageHeader = new ImageHeader(filePath);
            imageHeader.OnModelDataChange += this.HandleImageHeaderDataChange;
            imageHeader.OnClose += this.HandleImageHeaderClose;
            imageHeader.OnHeaderClick += this.HandleImageHeaderClick;
            return imageHeader;
        }

        protected void HandleImageHeaderDataChange() =>
            this.SetMapImage();

        protected void HandleImageHeaderClose(ImageHeader imageHeader)
        {
            var changingCurrent = (imageHeader == this.ImageHeaders.Current);
            this.ImageHeaders.Remove(imageHeader);
            if (this.ImageHeaders.IsEmpty)
                this.MapImage.Source = null;
        }

        protected void HandleImageHeaderClick(ImageHeader imageHeader)
        {
            if (!this.ImageHeaders.Feature(imageHeader))
                return;
            this.SetMapImage();
        }

        protected void HandleChangedImageHeader(ImageHeader imageHeader)
        {
            this.ImageHeaders.Each(x => x.Display((x == imageHeader)));
            this.SetMapImage();
        }

        protected void HandleChangedImageHeaderCollection(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.ImageHeaders.Count < MAXIMUM_IMAGE_HEADER_COUNT)
                this.SelectImageButton.Show();
            else
                this.SelectImageButton.Collapse();
        }

        protected void PressKey(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    this.Exit();
                    break;
                case Key.Right:
                    this.ImageHeaders++;
                    this.SetMapImage();
                    break;
                case Key.Left:
                    this.ImageHeaders--;
                    this.SetMapImage();
                    break;
                case Key.Z when (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)):
                    this.ActiveImageHeader.Undo(this, default);
                    break;
                case Key.Y when (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)):
                case Key.X when (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)):
                    this.ActiveImageHeader.Redo(this, default);
                    break;
                case Key.C when (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)):
                    this.ActiveImageHeader.Copy(this, default);
                    break;
                case Key.V when (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)):
                    this.ActiveImageHeader.Paste(this, default);
                    break;
            }
            e.Handled = true;
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
            if (this.ImageHeaders.Count >= MAXIMUM_IMAGE_HEADER_COUNT)
                return;
            var imageHeader = this.ConfigureNewImageHeader(QUICK_LOAD_PATH);
            this.ImageHeaders.Add(imageHeader, feature: true);
            this.SetMapImage();
        }

        protected void SelectImage(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = this.InitialFileDirectory;
            dialog.DefaultExt = Consts.FILE_EXTENSION_PNG;
            dialog.Filter = Consts.FILE_FILTER_PNG;
            dialog.Multiselect = true;
            if (dialog.ShowDialog().Not())
                return;

            foreach (var fileName in dialog.FileNames)
            {
                if (this.ImageHeaders.Count >= MAXIMUM_IMAGE_HEADER_COUNT)
                    break;
                var imageHeader = this.ConfigureNewImageHeader(fileName);
                this.ImageHeaders.Add(imageHeader, feature: true);
            }
            this.SetMapImage();
        }

        protected void SetMapImage()
        {
            if (this.ActiveImageData == null)
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

            this.MapImage.Source = this.ActiveImageData.ToBitmapImage();
            Task.Run(async () =>
            {
                var mupper = new Mupper();
                this.MapInfo = await mupper.InfoAsync(this.ActiveImageData);

                Application.Current.Dispatcher.Invoke(() => this.MapMemo = $"Colors: {this.MapInfo.NonEdgeColorSet.Count}");

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

        public void BackingClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Released)
                return;
            if (e.ChangedButton != MouseButton.Middle)
                return;
            this.BackingCellHelper.Collapse();
            e.Handled = true;
        }

        public void BackingMouseEnter(object sender, MouseEventArgs e)
        {
            if (this.BackingCellHelper.Data == null)
                return;
            this.MapImage.Source = this.BackingCellHelper.Data.ToBitmapImage();
            this.MapMemo = "Backing cells";
        }

        public void BackingMouseLeave(object sender, MouseEventArgs e)
        {
            this.MapImage.Source = this.ActiveImageData.ToBitmapImage();
        }

        public void BindingClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Released)
                return;
            if (e.ChangedButton != MouseButton.Middle)
                return;
            this.BindingCellHelper.Collapse();
            e.Handled = true;
        }

        public void BindingMouseEnter(object sender, MouseEventArgs e)
        {
            if (this.BindingCellHelper.Data == null)
                return;
            this.MapImage.Source = this.BindingCellHelper.Data.ToBitmapImage();
            this.MapMemo = "Binding cells";
        }

        public void BindingMouseLeave(object sender, MouseEventArgs e)
        {
            this.MapImage.Source = this.ActiveImageData.ToBitmapImage();
        }

        protected void CenterImage(object sender, RoutedEventArgs e)
        {
            this.MapImageZoomer.Reset();
        }

        protected async void LogImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            var targetFileName = this.GetTimeStampedFileName(".log");
            var targetFilePath = Path.Combine(this.ActiveImageHeader.FileDirectory, targetFileName);
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
            this.BackingCellHelper.Show();
            this.BackingCellHelper.Set(this.ActiveImageData, this.MapInfo);
        }

        protected void BindImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            this.BindingCellHelper.Show();
            this.BindingCellHelper.Set(this.ActiveImageData, this.MapInfo);
        }

        protected async void PopImage(object sender, RoutedEventArgs e)
        {
            if (this.ActiveImageData == null)
                return;
            using var scope = this.ScopedMupperImagingOperation();
            var bitmap = await scope.Value.PopAsync(this.ActiveImageData, this.BackingCellHelper.Data, this.BindingCellHelper.Data, POP_ARGB, ROOT_ARGB, IGNORED_ARGB_SET);
            this.ActiveImage.Advance(bitmap.ToPNG());
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