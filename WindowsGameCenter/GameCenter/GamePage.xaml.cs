using GameCenter.Helpers;
using GameCenter.Helpers.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace GameCenter
{
    public sealed partial class GamePage : Page
    {
        public ObservableCollection<Game> SteamGames { get; } = new ObservableCollection<Game>();
        public ObservableCollection<Game> XboxGames { get; } = new ObservableCollection<Game>();
        public ObservableCollection<Game> EpicGames { get; } = new ObservableCollection<Game>();
        public ObservableCollection<Game> OtherGames { get; } = new ObservableCollection<Game>();
        public ObservableCollection<GamePath> CustomPaths { get; } = new ObservableCollection<GamePath>();

        private GameScanner _gameScanner;
        private bool _isScanning = false;
        private bool _isFirstRun = true;
        private AddGamePathDialog _addPathDialog;

        public GamePage()
        {
            this.InitializeComponent();

            // Get the window from the app
            var window = (Application.Current as App)?.MainWindow;

            // Create the game scanner with the window reference
            _gameScanner = new GameScanner(window);

            // Subscribe to permission status changes
            _gameScanner.PermissionStatusChanged += GameScanner_PermissionStatusChanged;

            // Load games when the page is loaded
            Loaded += GamePage_Loaded;
        }

        private void GameScanner_PermissionStatusChanged(object sender, bool hasPermission)
        {
            // Update UI based on permission status
            // Make sure this runs on the UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                // Only show the warning if we definitely don't have permission
                // If we have partial permission, don't show the warning
                PermissionWarning.IsOpen = !hasPermission;
                System.Diagnostics.Debug.WriteLine($"Permission warning visibility set to: {!hasPermission}");
            });
        }

        private async void GamePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Show loading indicator
            LoadingIndicator.IsActive = true;

            // Check for permission on first run
            if (_isFirstRun)
            {
                _isFirstRun = false;

                // Check permission status
                bool hasPermission = await _gameScanner.CheckFileSystemAccessAsync();

                // Explicitly set the InfoBar visibility based on permission
                PermissionWarning.IsOpen = !hasPermission;
                System.Diagnostics.Debug.WriteLine($"Initial permission check: {hasPermission}, InfoBar open: {!hasPermission}");

                // Load custom paths
                await LoadCustomPathsAsync();
            }

            // Scan for games
            await ScanForGamesAsync();

            // Hide loading indicator
            LoadingIndicator.IsActive = false;
        }

        private async Task LoadCustomPathsAsync()
        {
            try
            {
                // Clear existing paths
                CustomPaths.Clear();

                // Get custom paths
                var paths = await _gameScanner.GetCustomPathsAsync();

                // Add to the collection
                foreach (var path in paths)
                {
                    CustomPaths.Add(path);
                }

                // Show or hide the custom paths section
                CustomPathsSection.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading custom paths: {ex.Message}");
            }
        }

        private async Task ScanForGamesAsync()
        {
            if (_isScanning)
                return;

            _isScanning = true;

            try
            {
                // Clear existing games
                SteamGames.Clear();
                XboxGames.Clear();
                EpicGames.Clear();
                OtherGames.Clear();

                // Scan for games by launcher type
                var gamesByLauncher = await _gameScanner.ScanForGamesAsync();

                // Process the games
                bool hasAnyGames = false;

                // Add Steam games
                if (gamesByLauncher.ContainsKey("Steam") && gamesByLauncher["Steam"].Count > 0)
                {
                    foreach (var game in gamesByLauncher["Steam"])
                    {
                        SteamGames.Add(game);
                    }
                    SteamGamesSection.Visibility = Visibility.Visible;
                    hasAnyGames = true;
                }
                else
                {
                    SteamGamesSection.Visibility = Visibility.Collapsed;
                }

                // Add Xbox games
                if (gamesByLauncher.ContainsKey("Xbox") && gamesByLauncher["Xbox"].Count > 0)
                {
                    foreach (var game in gamesByLauncher["Xbox"])
                    {
                        XboxGames.Add(game);
                    }
                    XboxGamesSection.Visibility = Visibility.Visible;
                    hasAnyGames = true;
                }
                else
                {
                    XboxGamesSection.Visibility = Visibility.Collapsed;
                }

                // Add Epic games
                if (gamesByLauncher.ContainsKey("Epic") && gamesByLauncher["Epic"].Count > 0)
                {
                    foreach (var game in gamesByLauncher["Epic"])
                    {
                        EpicGames.Add(game);
                    }
                    EpicGamesSection.Visibility = Visibility.Visible;
                    hasAnyGames = true;
                }
                else
                {
                    EpicGamesSection.Visibility = Visibility.Collapsed;
                }

                // Add Other games
                if (gamesByLauncher.ContainsKey("Other") && gamesByLauncher["Other"].Count > 0)
                {
                    foreach (var game in gamesByLauncher["Other"])
                    {
                        OtherGames.Add(game);
                    }
                    OtherGamesSection.Visibility = Visibility.Visible;
                    hasAnyGames = true;
                }
                else
                {
                    OtherGamesSection.Visibility = Visibility.Collapsed;
                }

                // Show or hide the no games message
                NoGamesMessage.Visibility = hasAnyGames ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                // Show error message
                ErrorMessage.Text = $"Error scanning for games: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
            }
            finally
            {
                _isScanning = false;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Show loading indicator
            LoadingIndicator.IsActive = true;

            // Hide error message
            ErrorMessage.Visibility = Visibility.Collapsed;

            // Reload custom paths
            await LoadCustomPathsAsync();

            // Scan for games
            await ScanForGamesAsync();

            // Hide loading indicator
            LoadingIndicator.IsActive = false;
        }

        private async void RequestPermissionButton_Click(object sender, RoutedEventArgs e)
        {
            // Show loading indicator
            LoadingIndicator.IsActive = true;

            try
            {
                // Request permission
                await _gameScanner.CheckFileSystemAccessAsync();

                // Open Windows settings if needed
                if (!_gameScanner.HasPermission)
                {
                    // Open the Windows Privacy Settings
                    await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
                }

                // Refresh the games list
                await ScanForGamesAsync();
            }
            catch (Exception ex)
            {
                // Show error message
                ErrorMessage.Text = $"Error requesting permission: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
            }
            finally
            {
                // Hide loading indicator
                LoadingIndicator.IsActive = false;
            }
        }

        private async void AddPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the window from the app
                var window = (Application.Current as App)?.MainWindow;

                // Create the dialog
                _addPathDialog = new AddGamePathDialog(window);
                _addPathDialog.XamlRoot = this.Content.XamlRoot;

                // Show the dialog
                var result = await _addPathDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // Add the path
                    await _gameScanner.AddCustomPathAsync(
                        _addPathDialog.GamePath,
                        _addPathDialog.GameName,
                        _addPathDialog.LauncherType);

                    // Reload custom paths
                    await LoadCustomPathsAsync();

                    // Rescan for games
                    await ScanForGamesAsync();
                }
            }
            catch (Exception ex)
            {
                // Show error message
                ErrorMessage.Text = $"Error adding custom path: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }

        private async void RemovePathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the path ID from the button tag
                var button = sender as Button;
                var pathId = button?.Tag?.ToString();

                if (!string.IsNullOrEmpty(pathId))
                {
                    // Remove the path
                    await _gameScanner.RemoveCustomPathAsync(pathId);

                    // Reload custom paths
                    await LoadCustomPathsAsync();

                    // Rescan for games
                    await ScanForGamesAsync();
                }
            }
            catch (Exception ex)
            {
                // Show error message
                ErrorMessage.Text = $"Error removing custom path: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }
    }
}

