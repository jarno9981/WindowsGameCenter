// GameCenterSetup.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace GameCenter.Pages
{
    public sealed partial class GameCenterSetup : Window
    {
        public GameCenterSetup()
        {
            this.InitializeComponent();
        }

        private async void CompleteSetup_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                await ShowErrorDialog("Please enter a username.");
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                await ShowErrorDialog("Please enter a password.");
                return;
            }

            if (!AgreementCheckBox.IsChecked.Value)
            {
                await ShowErrorDialog("You must accept the License Agreement to continue.");
                return;
            }

            var setupInfo = new GameCenterSetupInfo
            {
                Username = UsernameTextBox.Text,
                ConnectedDevices = new ConnectedDevices
                {
                    PC = PCCheckBox.IsChecked.Value,
                    Console = ConsoleCheckBox.IsChecked.Value,
                    Mobile = MobileCheckBox.IsChecked.Value
                },
                LicenseAgreementAccepted = true
            };

            await SaveSetupInfoAsync(setupInfo);
            this.Close();
        }

        private async Task SaveSetupInfoAsync(GameCenterSetupInfo setupInfo)
        {
            try
            {
                StorageFolder documentsFolder = await KnownFolders.DocumentsLibrary.CreateFolderAsync("GameCenter", CreationCollisionOption.OpenIfExists);
                StorageFile file = await documentsFolder.CreateFileAsync("centersetup.json", CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteTextAsync(file, JsonSerializer.Serialize(setupInfo, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Failed to save setup information: {ex.Message}");
            }
        }

        private async Task ShowErrorDialog(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK"
            };

            errorDialog.XamlRoot = this.Content.XamlRoot;
            await errorDialog.ShowAsync();
        }
    }

    public class GameCenterSetupInfo
    {
        public string Username { get; set; }
        public ConnectedDevices ConnectedDevices { get; set; }
        public bool LicenseAgreementAccepted { get; set; }
    }

    public class ConnectedDevices
    {
        public bool PC { get; set; }
        public bool Console { get; set; }
        public bool Mobile { get; set; }
    }
}