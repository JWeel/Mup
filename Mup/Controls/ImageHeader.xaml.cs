using System.ComponentModel;
using Microsoft.Win32;
using Mup.Extensions;
using Mup.Models;
using System;
using System.IO;
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

        public string InitialFileDirectory { get; set; }

        public string FileDirectory { get; set; }

        public ImageModel Model { get; set; }

        public string FileName
        {
            get => (string) this.GetValue(ImageHeader.FileNameProperty);
            set => this.SetValue(ImageHeader.FileNameProperty, value);
        }

        public static readonly DependencyProperty FileNameProperty = typeof(ImageHeader).Register(nameof(ImageHeader.FileName), string.Empty);

        public string FilePath => Path.Combine(this.FileDirectory.CoalesceToEmpty(), this.FileName);

        public event Action OnInit;

        public event Action OnModelDataChange;

        public event Action<ImageHeader> OnSelect;

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

            this.Init(dialog.FileName);
        }

        public void Init(string filePath)
        {
            this.FileName = Path.GetFileName(filePath);
            this.FileDirectory = Path.GetDirectoryName(filePath);
            var bytes = File.ReadAllBytes(filePath);

            this.Model = new ImageModel(bytes);
            this.Model.OnAdvance += () =>
            {
                this.DetermineButtonState();
                this.OnModelDataChange?.Invoke();
            };

            this.InitButton.Collapse();
            this.HeaderGrid.Show();

            this.OnInit?.Invoke();
        }

        public void Close(object sender, RoutedEventArgs args)
        {
            if (this.Model.IsModified)
            {
                var result = MessageBox.Show("Lose unsaved changes?", "Confirm", MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK)
                    return;
            }

            this.HeaderGrid.Collapse();
            this.InitButton.Show();

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
            dialog.DefaultExt = FILE_EXTENSION_PNG;
            dialog.Filter = FILE_FILTER_PNG;
            if (dialog.ShowDialog().Not())
                return;

            var filePath = dialog.FileName;
            this.FileName = Path.GetFileName(filePath);
            this.FileDirectory = Path.GetDirectoryName(filePath);
            this.Model.Save(filePath);
        }

        public void Copy(object sender, RoutedEventArgs args)
        {
            if (this.Model == null)
                return;
            this.Model.Data.ToClipboard();
        }

        public void Paste(object sender, RoutedEventArgs args)
        {
            if (this.Model == null)
                return;
            var dataObject = Clipboard.GetDataObject() as DataObject;
            if ((dataObject == null) || !dataObject.TryGetBitmap(out var data))
                return;
            this.Model.Advance(data);
        }

        public void Undo(object sender, RoutedEventArgs args)
        {
            if (this.Model == null)
                return;
            var data = this.Model.Undo();
            this.DetermineButtonState();
            this.OnModelDataChange?.Invoke();
        }

        public void Redo(object sender, RoutedEventArgs args)
        {
            if (this.Model == null)
                return;
            var data = this.Model.Redo();
            this.DetermineButtonState();
            this.OnModelDataChange?.Invoke();
        }

        public void Select(object sender, RoutedEventArgs args)
        {
            Console.WriteLine(this.FileName);
            this.OnSelect?.Invoke(this);
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