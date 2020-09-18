using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Mup
{
    public partial class Core : Window
    {
        #region Constructors

        // public Core()
        // {
        //     // this.InitializeComponent();
        // }

        #endregion

        #region Properties

        protected string SelectedFileName { get; set; }

        protected bool MovingWindow { get; set; }

        protected Point PositionBeforeMoving { get; set; }

        #endregion

        #region Methods

        protected void Pressed(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    System.Environment.Exit(0);
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

        protected void SelectFile(object sender, RoutedEventArgs a)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = @"d:\Downloads\Hymi\Next";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG Files (*.png)|*.png|All files (*.*)|*.*";

            if (dlg.ShowDialog() ?? false)
            {
                var fileName = dlg.FileName;
                this.SelectedFileName = fileName;
                this.Label_FileName.Content = fileName;
                var uri = new Uri(fileName, UriKind.Absolute);
                this.Image_Primary.Source = new BitmapImage(uri);

                this.Label_State.Content = "Pick option";
            }
        }

        #endregion
    }
}
