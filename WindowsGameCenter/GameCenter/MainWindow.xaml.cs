using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.System;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using Windows.Storage.Streams;
using GameCenter;
using Microsoft.UI.Xaml.Controls.Primitives;
using GameCenter.Helpers;
using System.ComponentModel.Design;
using GameCenter.Pages;

namespace GameCenter
{
    public sealed partial class MainWindow : CustomWindow, INotifyPropertyChanged
    {
        private DispatcherTimer _networkUpdateTimer;
        private List<long> _uploadSpeeds = new List<long>();
        private List<long> _downloadSpeeds = new List<long>();
        private long _lastBytesSent = 0;
        private long _lastBytesReceived = 0;
        private long _totalBytesSent = 0;
        private long _totalBytesReceived = 0;
        private List<Game> AllGames = new List<Game>();
        private AudioDeviceManager _audioDeviceManager;
        private DispatcherTimer _timer;
        private string _currentTime;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
                }
            }
        }

        public ObservableCollection<Game> RecommendedGames { get; } = new ObservableCollection<Game>();
        public ObservableCollection<Game> ExploreGames { get; } = new ObservableCollection<Game>();

        public MainWindow()
        {
            this.InitializeComponent();

            // Set up timer for updating the time display
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Start();

            // Initial time update
            UpdateTimeDisplay();

            InitializeNetworkMonitoring();
            InitializeAudioDevices();

            ContentFrame.Navigate(typeof(GamePage));
        }

        private void InitializeNetworkMonitoring()
        {
            UpdateNetworkIcon();
            UpdateNetworkInfo();

            _networkUpdateTimer = new DispatcherTimer();
            _networkUpdateTimer.Tick += NetworkUpdateTimer_Tick;
            _networkUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            _networkUpdateTimer.Start();
        }

        private void InitializeAudioDevices()
        {
            _audioDeviceManager = new AudioDeviceManager();
            _audioDeviceManager.LoadAudioDevicesAsync().ContinueWith(_ =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    AudioSourceComboBox.ItemsSource = _audioDeviceManager.AudioDevices;
                    AudioSourceComboBox.DisplayMemberPath = "Name";
                    AudioSourceComboBox.SelectedItem = _audioDeviceManager.DefaultDevice;
               
                });
            });
        }

        private void UpdateNetworkIcon()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile != null)
            {
                if (profile.NetworkAdapter.IanaInterfaceType == 6) // Ethernet
                {
                    NetworkIcon.Glyph = "\uE839"; // Ethernet icon
                }
                else if (profile.NetworkAdapter.IanaInterfaceType == 71) // Wi-Fi
                {
                    NetworkIcon.Glyph = "\uE701"; // Wi-Fi icon
                }
            }
        }

        private void UpdateNetworkInfo()
        {
            var hostNames = NetworkInformation.GetHostNames();
            var ipAddress = hostNames.FirstOrDefault(h => h.Type == Windows.Networking.HostNameType.Ipv4)?.DisplayName ?? "Unknown";

            DeviceNameText.Text = $"Device: {Environment.MachineName}";
            IpAddressText.Text = $"IP: {ipAddress}";
        }

        private void NetworkUpdateTimer_Tick(object sender, object e)
        {
            UpdateNetworkSpeeds();
            UpdateSpeedGraph();
        }

        private void UpdateNetworkSpeeds()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                         (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                          ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));

            long currentBytesSent = 0;
            long currentBytesReceived = 0;

            foreach (var networkInterface in networkInterfaces)
            {
                var stats = networkInterface.GetIPv4Statistics();
                currentBytesSent += stats.BytesSent;
                currentBytesReceived += stats.BytesReceived;
            }

            long uploadSpeed = currentBytesSent - _lastBytesSent;
            long downloadSpeed = currentBytesReceived - _lastBytesReceived;

            _uploadSpeeds.Add(uploadSpeed);
            _downloadSpeeds.Add(downloadSpeed);

            if (_uploadSpeeds.Count > 60) _uploadSpeeds.RemoveAt(0);
            if (_downloadSpeeds.Count > 60) _downloadSpeeds.RemoveAt(0);

            _lastBytesSent = currentBytesSent;
            _lastBytesReceived = currentBytesReceived;

            _totalBytesSent += uploadSpeed;
            _totalBytesReceived += downloadSpeed;

            UploadSpeedText.Text = $"{uploadSpeed / 1024.0 / 1024.0:F2} MB/s";
            DownloadSpeedText.Text = $"{downloadSpeed / 1024.0 / 1024.0:F2} MB/s";
     
        }

        private void UpdateSpeedGraph()
        {
            UpdateSpeedLine(UploadSpeedLine, _uploadSpeeds);
            UpdateSpeedLine(DownloadSpeedLine, _downloadSpeeds);
        }

        private void UpdateSpeedLine(Polyline line, List<long> speeds)
        {
            var points = new PointCollection();
            double maxSpeed = speeds.Max();
            for (int i = 0; i < speeds.Count; i++)
            {
                double x = i * (SpeedGraph.ActualWidth / 60);
                double y = SpeedGraph.ActualHeight - (speeds[i] / maxSpeed * SpeedGraph.ActualHeight);
                points.Add(new Windows.Foundation.Point(x, y));
            }
            line.Points = points;
        }



        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchFlyout.ShowAt(SearchButton);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = SearchBox.Text.ToLower();
            var filteredGames = AllGames.Where(game =>
                FuzzyMatch(game.Title, searchTerm) ||
                game.Launcher.ToLower().Contains(searchTerm)
            ).ToList();

            GameListView.ItemsSource = filteredGames;
        }

        private bool FuzzyMatch(string source, string target)
        {
            string pattern = string.Join(".*?", target.Select(c => Regex.Escape(c.ToString())));
            return Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase);
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Game game)
            {
                await Launcher.LaunchUriAsync(new Uri(game.LaunchUri));
            }
        }

        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            VolumeFlyout.ShowAt(VolumeButton);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            // TODO: Implement volume change logic
            // Note: Changing system volume might require additional permissions or APIs
        }

        private void AudioDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AudioSourceComboBox.SelectedItem is AudioDevice selectedDevice)
            {
                _audioDeviceManager.SetDefaultAudioDeviceAsync(selectedDevice);
            }
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            // This method is called when the account button is clicked
            // You can add additional logic here if needed
        }

        private void ConnectSteamAccount_Click(object sender, RoutedEventArgs e)
        {
            // Implement Steam account connection logic
        }

        private void ConnectEpicGamesAccount_Click(object sender, RoutedEventArgs e)
        {
            // Implement Epic Games account connection logic
        }

        private void ConnectUbisoftAccount_Click(object sender, RoutedEventArgs e)
        {
            // Implement Ubisoft account connection logic
        }

        private void ConnectXboxAccount_Click(object sender, RoutedEventArgs e)
        {
            // Implement Xbox account connection logic
        }

        private void ConnectRockstarAccount_Click(object sender, RoutedEventArgs e)
        {
            // Implement Rockstar account connection logic
        }

        private void Timer_Tick(object sender, object e)
        {
            UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        }

        private void NetworkButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void AudioSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AudioSourceComboBox.SelectedItem is AudioDevice selectedDevice)
            {
                _audioDeviceManager.SetDefaultAudioDeviceAsync(selectedDevice);
            }
        }

        private async void RestartWindows_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Restart Windows",
                Content = "Are you sure you want to restart Windows?",
                PrimaryButtonText = "Restart",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot,
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Add code to restart Windows
                // Note: This requires elevated permissions
                // You may need to use Windows.System.ShutdownManager or process.Start with shutdown commands
            }
        }

        private async void ShutdownWindows_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Shutdown Windows",
                Content = "Are you sure you want to shut down Windows?",
                PrimaryButtonText = "Shutdown",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot,

            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Add code to shutdown Windows
                // Note: This requires elevated permissions
                // You may need to use Windows.System.ShutdownManager or process.Start with shutdown commands
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if(NavView.SelectedItem == "Games") {
                ContentFrame.Navigate(typeof(GamePage));
            }
            else if(NavView.SelectedItem == "Friends")
            {
                ContentFrame.Navigate(typeof(Friends));
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                // Handle settings navigation if needed
                return;
            }

            var tag = args.InvokedItemContainer.Tag.ToString();
            NavView_Navigate(tag);
        }

        private void NavView_Navigate(string navItemTag)
        {
            Type pageType = null;
            switch (navItemTag)
            {
                case "Games":
                    pageType = typeof(GamePage);
                    break;
                case "Friends":
                    pageType = typeof(Friends);
                    break;
            }

            if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }

   

  
}

