using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace GameCenter.Helpers
{
    public sealed partial class AddGamePathDialog : ContentDialog
    {
        private string _selectedPath;
        private Window _window;

        public string GameName => GameNameTextBox.Text;
        public string GamePath => _selectedPath;
        public string LauncherType => (LauncherComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Other";

        public AddGamePathDialog(Window window)
        {
            this.InitializeComponent();
            _window = window;

            // Set the primary button as disabled initially
            IsPrimaryButtonEnabled = false;

            // Add event handlers for validation
            GameNameTextBox.TextChanged += ValidateInputs;
            LauncherComboBox.SelectionChanged += ValidateInputs;
        }

        private void ValidateInputs(object sender, object e)
        {
            // Enable the primary button only if all required fields are filled
            IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(GameNameTextBox.Text) &&
                                    !string.IsNullOrWhiteSpace(_selectedPath) &&
                                    LauncherComboBox.SelectedItem != null &&
                                    PathValidationMessage.Visibility == Visibility.Collapsed;
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add("*");

                // Initialize the folder picker with the window handle
                // Get the window handle for the current window
                var hwnd = WindowNative.GetWindowHandle(_window);

                // Initialize the folder picker with the window handle
                InitializeWithWindow.Initialize(folderPicker, hwnd);

                // Show the folder picker
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    _selectedPath = folder.Path;
                    GamePathTextBox.Text = _selectedPath;

                    // If the game name is empty, use the folder name
                    if (string.IsNullOrWhiteSpace(GameNameTextBox.Text))
                    {
                        GameNameTextBox.Text = folder.Name;
                    }

                    // Clear any validation messages
                    PathValidationMessage.Visibility = Visibility.Collapsed;

                    // Validate inputs
                    ValidateInputs(this, null);
                }
            }
            catch (Exception ex)
            {
                // Show error message
                PathValidationMessage.Text = $"Error selecting folder: {ex.Message}";
                PathValidationMessage.Visibility = Visibility.Visible;
                ValidateInputs(this, null);
            }
        }

        private void GamePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the entered path
            string enteredPath = GamePathTextBox.Text.Trim();

            // Validate the path
            if (string.IsNullOrWhiteSpace(enteredPath))
            {
                _selectedPath = null;
                PathValidationMessage.Visibility = Visibility.Collapsed;
            }
            else if (ValidatePath(enteredPath))
            {
                _selectedPath = enteredPath;
                PathValidationMessage.Visibility = Visibility.Collapsed;

                // If the game name is empty, use the folder name
                if (string.IsNullOrWhiteSpace(GameNameTextBox.Text))
                {
                    try
                    {
                        string folderName = new DirectoryInfo(enteredPath).Name;
                        GameNameTextBox.Text = folderName;
                    }
                    catch
                    {
                        // Ignore errors when getting folder name
                    }
                }
            }
            else
            {
                _selectedPath = null;
                PathValidationMessage.Text = "The specified path does not exist or is not accessible.";
                PathValidationMessage.Visibility = Visibility.Visible;
            }

            // Validate inputs
            ValidateInputs(this, null);
        }

        private bool ValidatePath(string path)
        {
            try
            {
                // Check if the path exists and is a directory
                return Directory.Exists(path);
            }
            catch
            {
                // If there's an exception, the path is invalid
                return false;
            }
        }
    }
}

