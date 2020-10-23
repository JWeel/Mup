using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Mup.Extensions
{
    public static class WPFExtensions
    {
        #region Visibility Extensions

        public static void Show(this UIElement element) =>
            element.Visibility = Visibility.Visible;

        public static void Hide(this UIElement element) =>
            element.Visibility = Visibility.Hidden;

        public static void Collapse(this UIElement element) =>
            element.Visibility = Visibility.Collapsed;

        #endregion

        #region Enable/Disable Extensions

        public static void Enable(this UIElement element) =>
            element.IsEnabled = true;
            
        public static void Disable(this UIElement element) =>
            element.IsEnabled = false;

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

        #region Remove Routed Event Handlers

        /// <summary>
        /// Removes all event handlers subscribed to the specified routed event from the specified element.
        /// </summary>
        /// <param name="element">The UI element on which the routed event is defined.</param>
        /// <param name="routedEvent">The routed event for which to remove the event handlers.</param>
        public static void RemoveRoutedEventHandlers(this UIElement element, RoutedEvent routedEvent)
        {
            // Get the EventHandlersStore instance which holds event handlers for the specified element.
            // The EventHandlersStore class is declared as internal.
            var eventHandlersStoreProperty = typeof(UIElement).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic);
            var eventHandlersStore = eventHandlersStoreProperty.GetValue(element, null);

            // If no event handlers are subscribed, eventHandlersStore will be null.
            // Credit: https://stackoverflow.com/a/16392387/1149773
            if (eventHandlersStore == null)
                return;

            // Invoke the GetRoutedEventHandlers method on the EventHandlersStore instance 
            // for getting an array of the subscribed event handlers.
            var getRoutedEventHandlers = eventHandlersStore.GetType().GetMethod("GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var routedEventHandlers = (RoutedEventHandlerInfo[]) getRoutedEventHandlers.Invoke(eventHandlersStore, routedEvent.IntoArray());

            // Iteratively remove all routed event handlers from the element.
            foreach (var routedEventHandler in routedEventHandlers)
                element.RemoveHandler(routedEvent, routedEventHandler.Handler);
        }

        #endregion

        #region Register

        /// <summary> Registers a dependency property for this type with a specified name and a specified default value for metadata. </summary>
        public static DependencyProperty Register<T>(this Type type, string name, T defaultValue) =>
            DependencyProperty.Register(name, typeof(T), type, new PropertyMetadata(defaultValue));
            
        #endregion
    }
}