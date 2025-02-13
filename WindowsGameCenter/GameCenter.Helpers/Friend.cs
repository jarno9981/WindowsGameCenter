using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCenter.Helpers
{
    public class Friend
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public SolidColorBrush StatusColor { get; set; }
        public string ProfilePicture { get; set; }
        public string Platform { get; set; }
        public string PlatformIcon { get; set; }
    }
}
