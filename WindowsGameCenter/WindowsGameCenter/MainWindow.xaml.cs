using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using System.Collections.ObjectModel;
using WinRT.Interop;

namespace WindowsGameCenter
{
    public sealed partial class MainWindow : CustomWindow
    {
        public ObservableCollection<Game> RecommendedGames { get; } = new();
        public ObservableCollection<Game> ExploreGames { get; } = new();

        public MainWindow()
        {
            this.InitializeComponent();

            // Set window size
            var windowId = Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this));
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1280, Height = 720 });

            // Initialize recommended games
            RecommendedGames.Add(new Game
            {
                Title = "Avengers: Endgame",
                Subtitle = "Dec 13, 2019 · Action/Sci-Fi · 3h 1m",
                ImageUrl = "ms-appx:///Assets/avengers.jpg"
            });
            RecommendedGames.Add(new Game
            {
                Title = "Oppenheimer",
                Subtitle = "2023 · Biography/Drama · 3h",
                ImageUrl = "ms-appx:///Assets/oppenheimer.jpg"
            });
            RecommendedGames.Add(new Game { Title = "Cyberpunk 2077", ImageUrl = "ms-appx:///Assets/cyberpunk.jpg" });
            RecommendedGames.Add(new Game { Title = "Red Dead Redemption 2", ImageUrl = "ms-appx:///Assets/rdr2.jpg" });
            RecommendedGames.Add(new Game { Title = "Elden Ring", ImageUrl = "ms-appx:///Assets/eldenring.jpg" });
            RecommendedGames.Add(new Game { Title = "God of War", ImageUrl = "ms-appx:///Assets/gow.jpg" });


            // Initialize explore games
            ExploreGames.Add(new Game
            {
                Title = "Spider-Man: No Way Home",
                ImageUrl = "ms-appx:///Assets/spiderman.jpg"
            });
            ExploreGames.Add(new Game
            {
                Title = "Dune: Part Two",
                ImageUrl = "ms-appx:///Assets/dune.jpg"
            });
            ExploreGames.Add(new Game { Title = "The Last of Us Part I", ImageUrl = "ms-appx:///Assets/tlou.jpg" });
            ExploreGames.Add(new Game { Title = "Starfield", ImageUrl = "ms-appx:///Assets/starfield.jpg" });
            ExploreGames.Add(new Game { Title = "Forza Horizon 5", ImageUrl = "ms-appx:///Assets/forza.jpg" });
            ExploreGames.Add(new Game { Title = "Halo Infinite", ImageUrl = "ms-appx:///Assets/halo.jpg" });
            ExploreGames.Add(new Game { Title = "Resident Evil 4", ImageUrl = "ms-appx:///Assets/re4.jpg" });
            ExploreGames.Add(new Game { Title = "Deathloop", ImageUrl = "ms-appx:///Assets/deathloop.jpg" });
        }
    }
}

