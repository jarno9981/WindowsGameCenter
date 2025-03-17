using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCenter.Helpers
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                // If it's the default date or very old, show "Never played"
                if (dateTime == DateTime.MinValue || dateTime < DateTime.Now.AddYears(-10))
                    return "Never played";

                // If it's today
                if (dateTime.Date == DateTime.Today)
                    return "Today";

                // If it's yesterday
                if (dateTime.Date == DateTime.Today.AddDays(-1))
                    return "Yesterday";

                // If it's within the last week
                if (dateTime > DateTime.Now.AddDays(-7))
                    return $"{(int)(DateTime.Now - dateTime).TotalDays} days ago";

                // Otherwise, show the date
                return $"Last played {dateTime:MMM d}";
            }

            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
