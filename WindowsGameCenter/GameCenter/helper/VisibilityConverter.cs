using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCenter.helper
{
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return Visibility.Collapsed;

            if (value is string stringValue)
                return string.IsNullOrEmpty(stringValue) ? Visibility.Collapsed : Visibility.Visible;

            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;

            // If it's not null, it's visible
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (targetType == typeof(bool) && value is Visibility visibility)
                return visibility == Visibility.Visible;

            throw new NotImplementedException();
        }
    }
}
