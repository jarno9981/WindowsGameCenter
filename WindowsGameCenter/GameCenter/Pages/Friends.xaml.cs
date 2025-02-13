using GameCenter.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using Windows.UI;

namespace GameCenter.Pages
{
    public sealed partial class Friends : Page
    {
        public ObservableCollection<Friend> Friendes { get; } = new ObservableCollection<Friend>();

        public Friends()
        {
            this.InitializeComponent();
            LoadFriends();
        }

        private void LoadFriends()
        {
            // Sample data with actual platform icons
            Friendes.Add(new Friend
            {
                Name = "John Doe",
                Status = "Online",
                StatusColor = new SolidColorBrush(Colors.Green),
                ProfilePicture = "ms-appx:///Assets/Profiles/john.png",
                Platform = "Steam",
                PlatformIcon = "ms-appx:///Assets/PlatformIcons/steam.png"
            });

            Friendes.Add(new Friend
            {
                Name = "Jane Smith",
                Status = "In Game",
                StatusColor = new SolidColorBrush(Colors.Purple),
                ProfilePicture = "ms-appx:///Assets/Profiles/jane.png",
                Platform = "Xbox",
                PlatformIcon = "ms-appx:///Assets/PlatformIcons/xbox.png"
            });

            Friendes.Add(new Friend
            {
                Name = "Bob Johnson",
                Status = "Offline",
                StatusColor = new SolidColorBrush(Colors.Gray),
                ProfilePicture = "ms-appx:///Assets/Profiles/bob.png",
                Platform = "PlayStation",
                PlatformIcon = "ms-appx:///Assets/PlatformIcons/PlayStation.png"
            });

            Friendes.Add(new Friend
            {
                Name = "Alice Williams",
                Status = "Online",
                StatusColor = new SolidColorBrush(Colors.Green),
                ProfilePicture = "ms-appx:///Assets/Profiles/alice.png",
                Platform = "Epic Games",
                PlatformIcon = "ms-appx:///Assets/PlatformIcons/Epic Games.png"
            });

            Friendes.Add(new Friend
            {
                Name = "Charlie Brown",
                Status = "Away",
                StatusColor = new SolidColorBrush(Colors.Orange),
                ProfilePicture = "ms-appx:///Assets/Profiles/charlie.png",
                Platform = "Ubisoft",
                PlatformIcon = "ms-appx:///Assets/PlatformIcons/ubisoft.png"
            });

            Friendes.Add(new Friend
            {
                Name = "David Wilson",
                Status = "In Game",
                StatusColor = new SolidColorBrush(Colors.Purple),
                ProfilePicture = "ms-appx:///Assets/Profiles/david.png",
                Platform = "Rockstar",
                PlatformIcon = "ms-appx:///Assets/PlatformIcons/Rockstar Social Club.png"
            });
        }
    }

}