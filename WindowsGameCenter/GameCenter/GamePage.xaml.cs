using GameCenter.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SQLitePCL;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace GameCenter
{
    public sealed partial class GamePage : Page
    {
        public ObservableCollection<Game> RecommendedGames { get; } = new ObservableCollection<Game>();
        public ObservableCollection<Game> ExploreGames { get; } = new ObservableCollection<Game>();
        private DatabaseHelper _databaseHelper;

        public GamePage()
        {
            this.InitializeComponent();
           // _databaseHelper = new DatabaseHelper();
           //Loaded += GamePage_Loaded;
        }

        private async void GamePage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize batteries

                await LoadGamesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GamePage_Loaded: {ex.Message}");
                await ShowErrorMessageAsync("Failed to load games. Please try again later.");
            }
        }

        private async Task LoadGamesAsync()
        {
            try
            {
                Debug.WriteLine("Starting LoadGamesAsync");
                RecommendedGames.Clear();
                ExploreGames.Clear();

                var games = _databaseHelper.LoadGames();
                Debug.WriteLine($"Loaded {games.Count} games from the database.");

                if (games.Count == 0)
                {
                    Debug.WriteLine("No games were loaded from the database.");
                    await ShowErrorMessageAsync("No games found in the database.");
                    return;
                }

                var sortedGames = games.OrderByDescending(g => g.LastPlayed).ToList();

                foreach (var game in sortedGames)
                {
                    // Ensure image sources are null
                    game.ImageSource = null;
                    game.ScreenshotSources.Clear();

                    if (RecommendedGames.Count < 4)
                    {
                        RecommendedGames.Add(game);
                    }
                    else
                    {
                        ExploreGames.Add(game);
                    }
                }

                Debug.WriteLine($"Loaded {RecommendedGames.Count} recommended games and {ExploreGames.Count} explore games.");

                // Check if any games were added to the collections
                if (RecommendedGames.Count == 0 && ExploreGames.Count == 0)
                {
                    Debug.WriteLine("No games were added to RecommendedGames or ExploreGames collections.");
                    await ShowErrorMessageAsync("Failed to load games into the UI. Please check the database content.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadGamesAsync: {ex.Message}");
                await ShowErrorMessageAsync($"Failed to load games. Error: {ex.Message}");
            }
        }

        private async Task DeleteGameAsync(Game game)
        {
            try
            {
                RecommendedGames.Remove(game);
                ExploreGames.Remove(game);

                _databaseHelper.DeleteGame(game.Id);

                if (!string.IsNullOrEmpty(game.ImageUrl))
                {
                    try
                    {
                        var imageFile = await StorageFile.GetFileFromPathAsync(game.ImageUrl);
                        await imageFile.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting game image: {ex.Message}");
                    }
                }

                await ShowErrorMessageAsync($"{game.Title} has been removed from your library.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting game: {ex.Message}");
                throw;
            }
        }

        private async void DeleteGame_Click(object sender, RoutedEventArgs e)
        {
            var game = (sender as FrameworkElement)?.DataContext as Game;
            if (game != null)
            {
                try
                {
                    await DeleteGameAsync(game);
                }
                catch (Exception ex)
                {
                    await ShowErrorMessageAsync($"Failed to delete {game.Title}. Please try again.");
                }
            }
        }

        private async void PlayGame_Click(object sender, RoutedEventArgs e)
        {
            var game = (sender as FrameworkElement)?.DataContext as Game;
            if (game != null)
            {
                try
                {
                    game.LastPlayed = DateTime.Now;
                    _databaseHelper.UpdateGame(game);

                    await Launcher.LaunchUriAsync(new Uri(game.LaunchUri));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error launching game: {ex.Message}");
                    await ShowErrorMessageAsync($"Failed to launch {game.Title}. Please try again.");
                }
            }
        }

        private async void DLC_Click(object sender, RoutedEventArgs e)
        {
            var dlc = (sender as FrameworkElement)?.DataContext as DLC;
            if (dlc != null)
            {
                try
                {
                    // Assuming you have a method to handle DLC purchase in DatabaseHelper
                    await ShowErrorMessageAsync($"Successfully purchased {dlc.Title}!");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error purchasing DLC: {ex.Message}");
                    await ShowErrorMessageAsync($"Failed to purchase {dlc.Title}. Please try again.");
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainScrollViewer.Visibility = Visibility.Visible;
        }

        private async Task ShowErrorMessageAsync(string message)
        {
            try
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Message",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await errorDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing dialog: {ex.Message}");
            }
        }
    }
}