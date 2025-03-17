using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCenter.Helpers
{
    public static class VisibilityHelper
    {
        /// <summary>
        /// Returns Visibility.Visible if the string value is not null or empty, otherwise returns Visibility.Collapsed
        /// </summary>
        /// <param name="value">The string value to check</param>
        /// <returns>Visibility.Visible or Visibility.Collapsed</returns>
        public static Visibility GetVisibility(string value)
        {
            return string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Returns Visibility.Visible if the object is not null, otherwise returns Visibility.Collapsed
        /// </summary>
        /// <param name="value">The object to check</param>
        /// <returns>Visibility.Visible or Visibility.Collapsed</returns>
        public static Visibility GetVisibility(object value)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Returns Visibility.Visible if the condition is true, otherwise returns Visibility.Collapsed
        /// </summary>
        /// <param name="condition">The condition to check</param>
        /// <returns>Visibility.Visible or Visibility.Collapsed</returns>
        public static Visibility GetVisibility(bool condition)
        {
            return condition ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Returns the inverse visibility (Collapsed if visible, Visible if collapsed)
        /// </summary>
        /// <param name="visibility">The visibility to invert</param>
        /// <returns>The inverted visibility</returns>
        public static Visibility InvertVisibility(Visibility visibility)
        {
            return visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
