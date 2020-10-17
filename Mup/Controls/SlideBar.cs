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

        public int Minimum { get; set; }

        public int Maximum { get; set; }

        public int Value { get; set; }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(SlideBar.Minimum), typeof(int), typeof(SlideBar), new PropertyMetadata(0));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(SlideBar.Maximum), typeof(int), typeof(SlideBar), new PropertyMetadata(0));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(SlideBar.Value), typeof(int), typeof(SlideBar), new PropertyMetadata(0));

        #endregion
    }
}