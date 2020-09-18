using System.Windows;

namespace Mup.Extensions
{
    public static class UIElementExtensions
    {
        #region Visibility Extensions

        public static void Show(this UIElement element) =>
            element.Visibility = Visibility.Visible;

        public static void Hide(this UIElement element) =>
            element.Visibility = Visibility.Hidden;

        public static void Collapse(this UIElement element) =>
            element.Visibility = Visibility.Collapsed;

        #endregion
    }
}