using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCenter.helper
{
    public class PlayTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int playTime)
            {
                if (playTime <= 0)
                    return "Never played";

                if (playTime < 60)
                    return $"{playTime} minutes";

                int hours = playTime / 60;
                int minutes = playTime % 60;

                if (minutes == 0)
                    return $"{hours} hours";

                return $"{hours} hours {minutes} minutes";
            }

            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
