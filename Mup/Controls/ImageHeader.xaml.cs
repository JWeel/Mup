using Microsoft.Win32;
using Mup.Extensions;
using Mup.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mup.Controls
{
    public partial class ImageHeader : UserControl
    {
        #region Constants

        private const string MODIFICATION_SUFFIX = "*";

        #endregion

        #region Constructor

        public ImageHeader(string filePath)
        {
            this.InitializeComponent();
            this.DataContext = this;

            this.FileName = Path.GetFileNameWithoutExtension(filePath);
            this.FileDirectory = Path.GetDirectoryName(filePath);
            var bytes = File.ReadAllBytes(filePath);

            this.Model = new ImageModel(bytes);
            this.Model.OnChangedCurrent += () =>
            {
                this.DetermineButtonState();
                this.ModificationSuffix = this.Model.IsModified ? MODIFICATION_SUFFIX : string.Empty;
                this.OnModelDataChange?.Invoke();
            };
        }

        #endregion

        #region Properties

        public string InitialFileDirectory { get; set; }

        public string FileDirectory { get; set; }

        public ImageModel Model { get; set; }

        public string FileName
        {
            get => (string) this.GetValue(ImageHeader.FileNameProperty);
            set => this.SetValue(ImageHeader.FileNameProperty, value);
        }

        public static readonly DependencyProperty FileNameProperty = typeof(ImageHeader).Register(nameof(ImageHeader.FileName), string.Empty);

        public string ModificationSuffix
        {
            get => (string) this.GetValue(ImageHeader.ModificationSuffixProperty);
            set => this.SetValue(ImageHeader.ModificationSuffixProperty, value);
        }

        public static readonly DependencyProperty ModificationSuffixProperty = typeof(ImageHeader).Register(nameof(ImageHeader.ModificationSuffix), string.Empty);

        public string FilePath => Path.Combine(this.FileDirectory.CoalesceToEmpty(), this.FileName + Consts.FILE_EXTENSION_PNG);

        public event Action OnModelDataChange;

        public event Action<ImageHeader> OnClose;

        public event Action<ImageHeader> OnHeaderClick;

        #endregion

        #region Methods

        public void HeaderClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed)
                return;

            if (e.ChangedButton == MouseButton.Middle)
            {
                e.Handled = true;
                this.Close(this, default);
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                e.Handled = true;
                this.OnHeaderClick?.Invoke(this);
            }
        }

        public void Close(object sender, RoutedEventArgs args)
        {
            if (this.Model.IsModified)
            {
                var result = MessageBox.Show("Lose unsaved changes?", "Confirm", MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK)
                    return;
            }

            this.OnClose?.Invoke(this);
        }

        public void Save(object sender, RoutedEventArgs args)
        {
            if (!this.Model.IsModified)
                return;
            this.Model.Save(this.FilePath);
        }

        public void SaveAs(object sender, RoutedEventArgs args)
        {
            var dialog = new SaveFileDialog();
            dialog.InitialDirectory = this.InitialFileDirectory;
            dialog.DefaultExt = Consts.FILE_EXTENSION_PNG;
            dialog.Filter = Consts.FILE_FILTER_PNG;
            if (dialog.ShowDialog().Not())
                return;

            var filePath = dialog.FileName;
            this.FileName = Path.GetFileNameWithoutExtension(filePath);
            this.FileDirectory = Path.GetDirectoryName(filePath);
            this.Model.Save(filePath);
        }

        public void Copy(object sender, RoutedEventArgs args)
        {
            this.CopyButton.Disable();
            this.Model.Data.ToClipboard();
            Task.Run(async () =>
            {
                await Task.Delay(150);
                Dispatcher.Invoke(() => this.CopyButton.Enable());
            });
        }

        public void Paste(object sender, RoutedEventArgs args)
        {
            var dataObject = Clipboard.GetDataObject() as DataObject;
            if ((dataObject == null) || !dataObject.TryGetBitmap(out var data))
                return;
            this.Model.Advance(data);
        }

        public void Undo(object sender, RoutedEventArgs args)
        {
            this.Model.Undo();
        }

        public void Redo(object sender, RoutedEventArgs args)
        {
            this.Model.Redo();
        }

        public void Display(bool state)
        {
            this.HeaderButton.IsEnabled = !state;
            if (state)
                this.OptionPanel.Show();
            else
                this.OptionPanel.Collapse();
        }

        protected void DetermineButtonState()
        {
            this.UndoButton.IsEnabled = !this.Model.IsStartOfTimeline;
            this.RedoButton.IsEnabled = !this.Model.IsEndOfTimeline;
            this.SaveButton.IsEnabled = this.Model.IsModified;
        }

        #endregion
    }
}