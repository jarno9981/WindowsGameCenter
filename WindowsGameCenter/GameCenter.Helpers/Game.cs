using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCenter.Helpers
{
    public class Game
    {
        public int Id { get; set; }
        public UInt32 AppId { get; set; }

        public UInt32 PlaytimeForever { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string LaunchUri { get; set; }
        public string Launcher { get; set; }
        public DateTime LastPlayed { get; set; }
        public List<string> Screenshots { get; set; }
        public string ExecutablePath { get; set; }
        public string InstallLocation { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string Developer { get; set; }
        public string ReleaseDate { get; set; }
        public string Genre { get; set; }
        public int PlayTime { get; set; }

        // Properties for UI
        public BitmapImage ImageSource { get; set; }
        public List<BitmapImage> ScreenshotSources { get; set; } = new List<BitmapImage>();
        public ObservableCollection<DLC> AvailableDLC { get; set; } = new ObservableCollection<DLC>();
    }
}
