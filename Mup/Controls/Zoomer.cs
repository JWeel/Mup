using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mup.Controls
{
    // adapted from https://stackoverflow.com/a/6782715/2946352
    public class Zoomer : Border
    {
        #region Constants

        private const double MIN_ZOOM = .5;
        private const double MAX_ZOOM = 290.0;
        private const double DELTA_ZOOM = .2;

        #endregion

        #region Properties

        public new UIElement Child
        {
            get => base.Child;
            set
            {
                if ((value != null) && (value != this.Child))
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public event Action<Point> MapMousePointChanged;

        private Point _mapMousePoint;
        public Point MapMousePoint
        {
            get => _mapMousePoint;
            set
            {
                _mapMousePoint = value;
                this.MapMousePointChanged?.Invoke(value);
            }
        }

        protected Point MoveOrigin { get; set; }

        protected Point MoveStart { get; set; }

        #endregion

        #region Public Methods

        public void Initialize(UIElement element)
        {
            if (element == null)
                return;
            var group = new TransformGroup();
            var st = new ScaleTransform();
            var tt = new TranslateTransform();
            group.Children.Add(st);
            group.Children.Add(tt);
            element.RenderTransform = group;
            this.MouseWheel += this.HandleMouseWheel;
            this.MouseLeftButtonDown += this.HandleLeftMouseDown;
            this.MouseLeftButtonUp += this.HandleLeftMouseUp;
            this.MouseMove += this.HandleMouseMove;
        }

        public void Reset()
        {
            if (this.Child == null)
                return;

            // reset zoom
            var st = this.GetScaleTransform(this.Child);
            st.ScaleX = 1.0;
            st.ScaleY = 1.0;

            // reset pan
            var tt = this.GetTranslateTransform(this.Child);
            tt.X = 0.0;
            tt.Y = 0.0;
        }

        #endregion

        #region Helper Methods

        protected TranslateTransform GetTranslateTransform(UIElement element) =>
            ((TransformGroup) element.RenderTransform).Children.OfType<TranslateTransform>().First();

        protected ScaleTransform GetScaleTransform(UIElement element) =>
            ((TransformGroup) element.RenderTransform).Children.OfType<ScaleTransform>().First();

        #endregion

        #region Child Events

        protected void HandleMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.Child == null)
                return;

            var st = this.GetScaleTransform(this.Child);
            var tt = this.GetTranslateTransform(this.Child);

            var zoom = (e.Delta > 0) ? DELTA_ZOOM : -DELTA_ZOOM;
            if (((e.Delta < 0) && ((st.ScaleX < MIN_ZOOM) || (st.ScaleY < MIN_ZOOM)))
                || ((e.Delta > 0) && ((st.ScaleX > MAX_ZOOM) || (st.ScaleY > MAX_ZOOM))))
                return;

            var relative = e.GetPosition(this.Child);
            var absoluteX = relative.X * st.ScaleX + tt.X;
            var absoluteY = relative.Y * st.ScaleY + tt.Y;

            st.ScaleX += zoom * st.ScaleX;
            st.ScaleY += zoom * st.ScaleY;
            tt.X = absoluteX - relative.X * st.ScaleX;
            tt.Y = absoluteY - relative.Y * st.ScaleY;
        }

        protected void HandleLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.Child == null)
                return;
            var tt = this.GetTranslateTransform(this.Child);
            this.MoveStart = e.GetPosition(this);
            this.MoveOrigin = new Point(tt.X, tt.Y);
            this.Cursor = Cursors.Hand;
            this.Child.CaptureMouse();
        }

        protected void HandleLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.Child == null)
                return;
            this.Child.ReleaseMouseCapture();
            this.Cursor = Cursors.Arrow;
        }

        protected void HandleMouseMove(object sender, MouseEventArgs e)
        {
            if (this.Child == null)
                return;

            var positionRelativeToChild = e.GetPosition(this.Child);
            this.MapMousePoint = this.GetRelativeMapMousePoint(positionRelativeToChild);

            if (!this.Child.IsMouseCaptured)
                return;

            var tt = this.GetTranslateTransform(this.Child);
            var displacement = this.MoveStart - e.GetPosition(this);
            tt.X = this.MoveOrigin.X - displacement.X;
            tt.Y = this.MoveOrigin.Y - displacement.Y;
        }

        protected Point GetRelativeMapMousePoint(Point controlSpacePosition)
        {
            if (!(this.Child is Image image) || !image.IsMouseOver || !(image.Source is BitmapImage bitmapImage))
                return new Point(-1, -1);

            var srcWidth = image.Source.Width / (96 / bitmapImage.DpiX);
            var srcHeight = image.Source.Height / (96 / bitmapImage.DpiY);

            // Take control space and convert to image space
            var x = Math.Floor(controlSpacePosition.X * srcWidth / image.ActualWidth);
            var y = Math.Floor(controlSpacePosition.Y * srcHeight / image.ActualHeight);
            return new Point(x, y);
        }

        #endregion
    }
}