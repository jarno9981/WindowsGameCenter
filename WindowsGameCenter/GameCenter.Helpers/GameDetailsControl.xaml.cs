using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.System;

namespace GameCenter.Helpers
{
    public sealed partial class GameDetailsControl : UserControl
    {
        private static readonly string _defaultImageUrl = "ms-appx:///Assets/GamePlaceholder.png";
        private DateTimeConverter _dateTimeConverter = new DateTimeConverter();

        public static readonly DependencyProperty GameProperty =
            DependencyProperty.Register("Game", typeof(Game), typeof(GameDetailsControl), new PropertyMetadata(null, OnGameChanged));

        public Game Game
        {
            get { return (Game)GetValue(GameProperty); }
            set { SetValue(GameProperty, value); }
        }

        public GameDetailsControl()
        {
            this.InitializeComponent();
        }

        private static void OnGameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (GameDetailsControl)d;
            var game = (Game)e.NewValue;

            if (game != null)
            {
                control.UpdateGameImage(game);
                control.UpdateVisibility(game);
                control.UpdateLastPlayed(game);
            }
        }

        private void UpdateVisibility(Game game)
        {
            // Show developer text only if it's available
            DeveloperText.Visibility = !string.IsNullOrEmpty(game.Developer) ?
                Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateLastPlayed(Game game)
        {
            // Format the last played date
            LastPlayedText.Text = _dateTimeConverter.Convert(game.LastPlayed, typeof(string), null, null) as string;
        }

        private void UpdateGameImage(Game game)
        {
            try
            {
                // If we already have an ImageSource, use it
                if (game.ImageSource != null)
                {
                    GameImage.Source = game.ImageSource;
                    return;
                }

                // If we have an image URL, try to load it
                if (!string.IsNullOrEmpty(game.ImageUrl))
                {
                    try
                    {
                        // Create a bitmap image
                        var bitmap = new BitmapImage();

                        // Set the source
                        bitmap.UriSource = new Uri(game.ImageUrl);

                        // Set the image source
                        GameImage.Source = bitmap;

                        // Cache the image source
                        game.ImageSource = bitmap;

                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading image from URL {game.ImageUrl}: {ex.Message}");
                        // Continue to fallback
                    }
                }

                // Fallback: Use a platform-specific placeholder
                SetDefaultImage(game);
            }
            catch (Exception ex)
            {
                // Log error and show default image
                System.Diagnostics.Debug.WriteLine($"Error loading game image: {ex.Message}");

                // Set a default image
                SetDefaultImage(game);
            }
        }

        private void SetDefaultImage(Game game)
        {
            // Use a platform-specific placeholder
            var defaultImage = new BitmapImage();

            try
            {
                switch (game.Launcher?.ToLower())
                {
                    case "steam":
                        defaultImage.UriSource = new Uri("ms-appx:///Assets/SteamPlaceholder.png");
                        break;
                    case "xbox":
                        defaultImage.UriSource = new Uri("ms-appx:///Assets/XboxPlaceholder.png");
                        break;
                    case "epic":
                        defaultImage.UriSource = new Uri("ms-appx:///Assets/EpicPlaceholder.png");
                        break;
                    default:
                        defaultImage.UriSource = new Uri(_defaultImageUrl);
                        break;
                }
            }
            catch
            {
                // If we can't load the platform-specific placeholder, use the default
                defaultImage.UriSource = new Uri(_defaultImageUrl);
            }

            GameImage.Source = defaultImage;
            game.ImageSource = defaultImage;
        }

        // Add the missing event handlers
        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (Game == null)
                return;

            try
            {
                // Different launch methods based on launcher type
                switch (Game.Launcher?.ToLower())
                {
                    case "steam":
                        await LaunchSteamGame();
                        break;
                    case "epic":
                        await LaunchEpicGame();
                        break;
                    case "xbox":
                        await LaunchXboxGame();
                        break;
                    default:
                        await LaunchExecutable();
                        break;
                }
            }
            catch (Exception ex)
            {
                // Show error dialog
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Error Launching Game",
                    Content = $"Failed to launch {Game.Title}: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await errorDialog.ShowAsync();
            }
        }

        private void PlayButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Show the play button with animation
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(200))
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeIn, PlayButton);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");
            storyboard.Children.Add(fadeIn);
            storyboard.Begin();
        }

        private void PlayButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Hide the play button with animation
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(200))
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeOut, PlayButton);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");
            storyboard.Children.Add(fadeOut);
            storyboard.Begin();
        }

        private async Task LaunchSteamGame()
        {
            // Try to extract the Steam AppID
            uint appId = 0;

            // First check if we already have the AppID
            if (Game.AppId > 0)
            {
                appId = Game.AppId;
            }
            else
            {
                // Try to extract from the install path
                string installPath = Game.InstallLocation;

                if (!string.IsNullOrEmpty(installPath))
                {
                    // Check if the path contains "steamapps/common"
                    var match = Regex.Match(installPath, @"(.*?)[\\\/]steamapps[\\\/]common[\\\/]", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string steamPath = match.Groups[1].Value;
                        string gameFolder = new System.IO.DirectoryInfo(installPath).Name;

                        // Look for appmanifest files in the steamapps directory
                        string manifestsDir = System.IO.Path.Combine(steamPath, "steamapps");
                        if (System.IO.Directory.Exists(manifestsDir))
                        {
                            var manifestFiles = System.IO.Directory.GetFiles(manifestsDir, "appmanifest_*.acf");
                            foreach (var manifestFile in manifestFiles)
                            {
                                // Read the manifest file
                                string content = System.IO.File.ReadAllText(manifestFile);

                                // Check if this manifest is for our game
                                if (content.Contains($"\"installdir\"\t\t\"{gameFolder}\"") ||
                                    content.Contains($"\"installdir\"\t\t\"{gameFolder.ToLower()}\""))
                                {
                                    // Extract the AppID
                                    var appIdMatch = Regex.Match(content, "\"appid\"\\s+\"(\\d+)\"");
                                    if (appIdMatch.Success)
                                    {
                                        appId = uint.Parse(appIdMatch.Groups[1].Value);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // If we still don't have an AppID, try to extract from the executable path
                if (appId == 0 && !string.IsNullOrEmpty(Game.ExecutablePath))
                {
                    var fileNameMatch = Regex.Match(System.IO.Path.GetFileName(Game.ExecutablePath), @"(\d+)");
                    if (fileNameMatch.Success)
                    {
                        if (uint.TryParse(fileNameMatch.Groups[1].Value, out uint extractedId))
                        {
                            appId = extractedId;
                        }
                    }
                }
            }

            // Launch the game
            if (appId > 0)
            {
                // Use the steam:// protocol to launch the game
                await Launcher.LaunchUriAsync(new Uri($"steam://run/{appId}"));
            }
            else if (!string.IsNullOrEmpty(Game.ExecutablePath) && System.IO.File.Exists(Game.ExecutablePath))
            {
                // Fallback to launching the executable directly
                await LaunchExecutable();
            }
            else
            {
                throw new Exception("Could not determine Steam AppID and no executable found.");
            }
        }

        private async Task LaunchEpicGame()
        {
            // Try to launch using Epic Games URI if available
            if (!string.IsNullOrEmpty(Game.LaunchUri))
            {
                await Launcher.LaunchUriAsync(new Uri(Game.LaunchUri));
            }
            else if (!string.IsNullOrEmpty(Game.ExecutablePath) && System.IO.File.Exists(Game.ExecutablePath))
            {
                // Fallback to launching the executable directly
                await LaunchExecutable();
            }
            else
            {
                throw new Exception("No launch URI or executable found for Epic game.");
            }
        }

        private async Task LaunchXboxGame()
        {
            // Try to launch using Xbox URI if available
            if (!string.IsNullOrEmpty(Game.LaunchUri))
            {
                await Launcher.LaunchUriAsync(new Uri(Game.LaunchUri));
            }
            else if (!string.IsNullOrEmpty(Game.ExecutablePath) && System.IO.File.Exists(Game.ExecutablePath))
            {
                // Fallback to launching the executable directly
                await LaunchExecutable();
            }
            else
            {
                throw new Exception("No launch URI or executable found for Xbox game.");
            }
        }

        private async Task LaunchExecutable()
        {
            if (string.IsNullOrEmpty(Game.ExecutablePath) || !System.IO.File.Exists(Game.ExecutablePath))
            {
                throw new Exception("Game executable not found.");
            }

            // Launch the executable
            var options = new LauncherOptions();
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(Game.ExecutablePath);
            await Launcher.LaunchFileAsync(file, options);
        }
    }
}

