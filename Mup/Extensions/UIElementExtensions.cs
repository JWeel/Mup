using System.Collections.Generic;
using System.Linq;
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

        #region EnumerateAllChildren

        public static IEnumerable<T> EnumerateAllChildren<T>(this DependencyObject obj) where T : DependencyObject
        {
            if (obj == null)
                yield break;
            if (obj is T)
                yield return obj as T;
            foreach (var child in LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>())
                foreach (var grandchild in child.EnumerateAllChildren<T>())
                    yield return grandchild;
        }
            
        #endregion
    }
}