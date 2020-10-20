using System.Windows;
using System.Windows.Controls;

namespace Mup.Controls
{
    public partial class SlideBar : UserControl
    {
        #region Constructor

        public SlideBar()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        #endregion

        #region Properties

        public string Label { get; set; }

        public int Minimum
        {
            get => (int) this.GetValue(SlideBar.MinimumProperty);
            set => this.SetValue(SlideBar.MinimumProperty, value);
        }

        public int Maximum
        {
            get => (int) this.GetValue(SlideBar.MaximumProperty);
            set => this.SetValue(SlideBar.MaximumProperty, value);
        }

        public int Value
        {
            get => (int) this.GetValue(SlideBar.ValueProperty);
            set => this.SetValue(SlideBar.ValueProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(SlideBar.Minimum), typeof(int), typeof(SlideBar), new PropertyMetadata(0));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(SlideBar.Maximum), typeof(int), typeof(SlideBar), new PropertyMetadata(0));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(SlideBar.Value), typeof(int), typeof(SlideBar), new PropertyMetadata(0));

        #endregion
    }
}