﻿using Microsoft.Win32;
using Mup.Extensions;
using Mup.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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
            this.ContiguousFlagCheckBox.DataContext = this;

            this.MapImageZoomer.MapMousePointChanged += point =>
                this.MapInfo = $"Pixel Coordinate: {point.X:0}, {point.Y:0}"; ;
        }

        #endregion

        #region Properties

        protected bool QuickLoadDisabled { get; set; }

        protected string SelectedFileName { get; set; }

        protected string SelectedFileDirectory { get; set; }

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
                        this.ContiguousFlagCheckBox.Hide();
                        break;
                    case MupState.SelectOption:
                        this.SelectFileButton.Collapse();
                        this.FileNameTextBox.Show();
                        this.OptionGrid.Show();
                        this.MapInfoLabel.Show();
                        this.ContiguousFlagCheckBox.Show();
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
            if (this.QuickLoadDisabled)
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

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = new Uri(filePath, UriKind.Absolute);
            image.EndInit();

            this.MapImage.Source = image;
            this.State = MupState.SelectOption;
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
            this.State = MupState.SelectFile;
        }

        protected async void LogImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            var sourceFilePath = Path.Combine(this.SelectedFileDirectory, this.SelectedFileName);
            var targetFileName = Path.GetFileNameWithoutExtension(this.SelectedFileName) + ".log";
            var targetFilePath = Path.Combine(this.SelectedFileDirectory, targetFileName);
            var task = Task.Run(() => mupper.Log(sourceFilePath, targetFilePath));
            using (new Scope(this.DisableButtons, this.EnableButtons))
                await task;
        }

        protected async void RepaintImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            var sourceFilePath = Path.Combine(this.SelectedFileDirectory, this.SelectedFileName);
            var targetFileName = Path.GetFileNameWithoutExtension(this.SelectedFileName) + "_p.png";
            var targetFilePath = Path.Combine(this.SelectedFileDirectory, targetFileName);
            var task = Task.Run(() => mupper.Repaint(sourceFilePath, targetFilePath, this.ContiguousFlag));
            using (new Scope(this.DisableButtons, this.EnableButtons))
                await task;
            this.SelectFile(targetFilePath);
        }

        protected async void IslandsImage(object sender, RoutedEventArgs e)
        {
        }

        protected async void MergeImage(object sender, RoutedEventArgs e)
        {
        }

        protected async void BorderImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            var sourceFilePath = Path.Combine(this.SelectedFileDirectory, this.SelectedFileName);
            var targetFileName = Path.GetFileNameWithoutExtension(this.SelectedFileName) + "_b.png";
            var targetFilePath = Path.Combine(this.SelectedFileDirectory, targetFileName);
            var task = Task.Run(() => mupper.Border(sourceFilePath, targetFilePath, (1, 1, 1)));
            using (new Scope(this.DisableButtons, this.EnableButtons))
                await task;
            this.SelectFile(targetFilePath);
        }

        protected async void ExtractImage(object sender, RoutedEventArgs e)
        {
            var mupper = new Mupper();
            var sourceFilePath = Path.Combine(this.SelectedFileDirectory, this.SelectedFileName);
            var targetFileName = Path.GetFileNameWithoutExtension(this.SelectedFileName) + "_x.png";
            var targetFilePath = Path.Combine(this.SelectedFileDirectory, targetFileName);
            var task = Task.Run(() => mupper.Extract(sourceFilePath, targetFilePath));
            using (new Scope(this.DisableButtons, this.EnableButtons))
                await task;
            this.SelectFile(targetFilePath);
        }

        protected void DisableButtons()
        {
            this.QuickLoadDisabled = true;
            this.EnumerateAllChildren<Button>()
                .Each(button => button.IsEnabled = false);
        }

        protected void EnableButtons()
        {
            this.QuickLoadDisabled = false;
            this.EnumerateAllChildren<Button>()
                .Each(button => button.IsEnabled = true);
        }

        protected void Exit(object sender, RoutedEventArgs e) =>
            Environment.Exit(0);

        #endregion
    }
}
