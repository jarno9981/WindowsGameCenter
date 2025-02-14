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

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GamePage_Loaded: {ex.Message}");
                await ShowErrorMessageAsync("Failed to load games. Please try again later.");
            }
        }

      

        private async Task DeleteGameAsync(Game game)
        {
            try
            {
                RecommendedGames.Remove(game);
                ExploreGames.Remove(game);


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