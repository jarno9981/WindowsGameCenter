using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.System;
using GameCenter.Helpers.Models;
using System.IO;
using System.Diagnostics;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace GameCenter.Helpers
{
    public sealed partial class GameDetailsControl : UserControl
    {
        private static readonly string _defaultImageUrl = "ms-appx:///Assets/GamePlaceholder.png";
        private DateTimeConverter _dateTimeConverter = new DateTimeConverter();
        private GameScanner _gameScanner;

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
            _gameScanner = new GameScanner();
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
                        Debug.WriteLine($"Error loading image from URL {game.ImageUrl}: {ex.Message}");
                        // Continue to fallback
                    }
                }

                // Fallback: Use a platform-specific placeholder
                SetDefaultImage(game);
            }
            catch (Exception ex)
            {
                // Log error and show default image
                Debug.WriteLine($"Error loading game image: {ex.Message}");

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
                        string gameFolder = new DirectoryInfo(installPath).Name;

                        // Look for appmanifest files in the steamapps directory
                        string manifestsDir = Path.Combine(steamPath, "steamapps");
                        if (Directory.Exists(manifestsDir))
                        {
                            var manifestFiles = Directory.GetFiles(manifestsDir, "appmanifest_*.acf");
                            foreach (var manifestFile in manifestFiles)
                            {
                                // Read the manifest file
                                string content = File.ReadAllText(manifestFile);

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
                    var fileNameMatch = Regex.Match(Path.GetFileName(Game.ExecutablePath), @"(\d+)");
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
            else if (!string.IsNullOrEmpty(Game.ExecutablePath) && File.Exists(Game.ExecutablePath))
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
            else if (!string.IsNullOrEmpty(Game.ExecutablePath) && File.Exists(Game.ExecutablePath))
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
            else if (!string.IsNullOrEmpty(Game.ExecutablePath) && File.Exists(Game.ExecutablePath))
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
            if (string.IsNullOrEmpty(Game.ExecutablePath) || !File.Exists(Game.ExecutablePath))
            {
                throw new Exception("Game executable not found.");
            }

            // Launch the executable
            var options = new LauncherOptions();
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(Game.ExecutablePath);
            await Launcher.LaunchFileAsync(file, options);
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            // Show game info popup
            ShowGameInfoPopup();
        }

        private Popup _gameInfoPopup;

        private void ShowGameInfoPopup()
        {
            // Create the popup if it doesn't exist
            if (_gameInfoPopup == null)
            {
                CreateGameInfoPopup();
            }

            // Position the popup near the info button
            var infoButtonTransform = InfoButton.TransformToVisual(null);
            var infoButtonPosition = infoButtonTransform.TransformPoint(new Windows.Foundation.Point(0, 0));

            // Position the popup to the right of the info button
            _gameInfoPopup.HorizontalOffset = infoButtonPosition.X - 400 + InfoButton.ActualWidth;
            _gameInfoPopup.VerticalOffset = infoButtonPosition.Y + InfoButton.ActualHeight;

            // Show the popup
            _gameInfoPopup.IsOpen = true;
        }

        private void CreateGameInfoPopup()
        {
            // Create the popup first
            _gameInfoPopup = new Popup
            {
                IsLightDismissEnabled = true
            };

            // Set the XamlRoot property - this is crucial for WinUI 3
            _gameInfoPopup.XamlRoot = this.XamlRoot;

            // Create the content
            var grid = new Grid
            {
                Background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as Brush,
                BorderBrush = Application.Current.Resources["CardStrokeColorDefaultBrush"] as Brush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(24),
                Width = 400,
                MaxHeight = 600
            };

            // Add shadow
            grid.Shadow = new ThemeShadow();

            // Create rows
            var headerRow = new RowDefinition { Height = GridLength.Auto };
            var contentRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
            grid.RowDefinitions.Add(headerRow);
            grid.RowDefinitions.Add(contentRow);

            // Create header
            var headerGrid = new Grid();
            var headerText = new TextBlock
            {
                Text = "Game Information",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold
            };

            var closeButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0)
            };
            closeButton.Click += ClosePopup_Click;

            var closeIcon = new FontIcon
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = "\uE8BB"
            };
            closeButton.Content = closeIcon;

            headerGrid.Children.Add(headerText);
            headerGrid.Children.Add(closeButton);
            Grid.SetRow(headerGrid, 0);
            grid.Children.Add(headerGrid);

            // Create content
            var scrollViewer = new ScrollViewer
            {
                Margin = new Thickness(0, 16, 0, 0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scrollViewer, 1);

            var contentStack = new StackPanel();

            // Add game details
            AddGameDetailItem(contentStack, "App ID", Game.AppId.ToString());
            AddGameDetailItem(contentStack, "Install Location", Game.InstallLocation);
            AddGameDetailItem(contentStack, "Executable", Game.ExecutablePath);

            if (!string.IsNullOrEmpty(Game.Developer))
                AddGameDetailItem(contentStack, "Developer", Game.Developer);

            if (!string.IsNullOrEmpty(Game.Publisher))
                AddGameDetailItem(contentStack, "Publisher", Game.Publisher);

            if (!string.IsNullOrEmpty(Game.ReleaseDate))
                AddGameDetailItem(contentStack, "Release Date", Game.ReleaseDate);

            if (!string.IsNullOrEmpty(Game.Genres))
                AddGameDetailItem(contentStack, "Genres", Game.Genres);

            if (!string.IsNullOrEmpty(Game.LaunchUri))
                AddGameDetailItem(contentStack, "Launch URI", Game.LaunchUri);

            AddGameDetailItem(contentStack, "Last Played",
                _dateTimeConverter.Convert(Game.LastPlayed, typeof(string), null, null) as string);

            AddGameDetailItem(contentStack, "Play Time",
                FormatPlayTime(Game.PlayTime));

            if (!string.IsNullOrEmpty(Game.Description))
                AddGameDetailItem(contentStack, "Description", Game.Description);

            scrollViewer.Content = contentStack;
            grid.Children.Add(scrollViewer);

            // Set the popup content
            _gameInfoPopup.Child = grid;
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            if (_gameInfoPopup != null)
            {
                _gameInfoPopup.IsOpen = false;
            }
        }

        private void AddGameDetailItem(StackPanel parent, string label, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            var itemStack = new StackPanel
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            var labelText = new TextBlock
            {
                Text = label,
                Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };

            var valueText = new TextBlock
            {
                Text = value,
                Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };

            itemStack.Children.Add(labelText);
            itemStack.Children.Add(valueText);
            parent.Children.Add(itemStack);
        }

   
        private string FormatPlayTime(int minutes)
        {
            if (minutes < 60)
            {
                return $"{minutes} minutes played";
            }
            else
            {
                int hours = minutes / 60;
                int remainingMinutes = minutes % 60;

                if (remainingMinutes == 0)
                {
                    return $"{hours} {(hours == 1 ? "hour" : "hours")} played";
                }
                else
                {
                    return $"{hours} {(hours == 1 ? "hour" : "hours")} {remainingMinutes} {(remainingMinutes == 1 ? "minute" : "minutes")} played";
                }
            }
        }

        // Method to refresh game metadata if needed
        public async Task RefreshGameMetadataAsync()
        {
            if (Game == null)
                return;

            // If it's a Steam game, refresh the metadata
            if (Game.Launcher?.ToLower() == "steam" && Game.AppId > 0)
            {
                await _gameScanner.RefreshSteamMetadataAsync(Game);

                // Update the UI
                UpdateGameImage(Game);
                UpdateVisibility(Game);
                UpdateLastPlayed(Game);             
            }
        }
    }
}