using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using GameCenter.Helpers.Models;
using System.Runtime.InteropServices;

namespace GameCenter.Helpers
{
    // Steam API Helper class for fetching and caching game metadata
    public class SteamGameInfo
    {
        public int AppId { get; set; }
        public string Name { get; set; }
        public string HeaderImageUrl { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public List<string> Screenshots { get; set; } = new List<string>();
    }

    public class SteamApiHelper
    {
        private readonly HttpClient _httpClient;
        private const string SteamStoreApiUrl = "https://store.steampowered.com/api/appdetails?appids=";
        private readonly string _cacheDirectory;

        public SteamApiHelper(string cacheDirectory = null)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Change cache directory to Documents folder
            if (cacheDirectory == null)
            {
                _cacheDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "GameCenter", "Steam");
            }
            else
            {
                _cacheDirectory = cacheDirectory;
            }

            // Ensure cache directory exists
            if (!Directory.Exists(_cacheDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_cacheDirectory);
                    Debug.WriteLine($"Created cache directory: {_cacheDirectory}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating cache directory: {ex.Message}");

                    // Fallback to AppData if Documents fails
                    _cacheDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "GameCenter", "SteamCache");

                    if (!Directory.Exists(_cacheDirectory))
                    {
                        Directory.CreateDirectory(_cacheDirectory);
                    }
                }
            }
        }

        // Rest of SteamApiHelper implementation remains the same
        // ...

        /// <summary>
        /// Gets a Steam game's information by its app ID, using local cache if available
        /// </summary>
        /// <param name="appId">The Steam app ID</param>
        /// <param name="forceRefresh">Whether to force a refresh from the API instead of using cache</param>
        /// <returns>A SteamGameInfo object containing the game's information</returns>
        public async Task<SteamGameInfo> GetGameInfoAsync(int appId, bool forceRefresh = false)
        {
            string metadataCachePath = Path.Combine(_cacheDirectory, $"{appId}.bin");

            // Check if we have cached data and should use it
            if (!forceRefresh && File.Exists(metadataCachePath))
            {
                try
                {
                    // Read from cache
                    byte[] cachedData = await File.ReadAllBytesAsync(metadataCachePath);
                    var cachedGameInfo = DeserializeGameInfo(cachedData);

                    // Verify the cached data has the correct app ID
                    if (cachedGameInfo != null && cachedGameInfo.AppId == appId)
                    {
                        Debug.WriteLine($"Using cached data for app ID {appId}");
                        return cachedGameInfo;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error reading cache for app ID {appId}: {ex.Message}");
                    // Continue to fetch from API if cache read fails
                }
            }

            // Fetch from API
            var steamGameInfo = await FetchGameInfoFromApiAsync(appId);

            // Save to cache
            if (steamGameInfo != null)
            {
                await SaveGameInfoToCacheAsync(steamGameInfo);
            }

            return steamGameInfo;
        }

        /// <summary>
        /// Fetches game information from the Steam API
        /// </summary>
        private async Task<SteamGameInfo> FetchGameInfoFromApiAsync(int appId)
        {
            try
            {
                // Make request to Steam Store API
                string url = $"{SteamStoreApiUrl}{appId}";
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Parse the JSON response
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(jsonResponse);

                // Navigate through the JSON structure to get the data
                JsonElement root = document.RootElement;
                JsonElement appData = root.GetProperty(appId.ToString());

                // Check if the request was successful
                if (!appData.GetProperty("success").GetBoolean())
                {
                    throw new Exception("Steam API returned unsuccessful response");
                }

                // Get the data object containing game details
                JsonElement data = appData.GetProperty("data");

                var gameInfo = new SteamGameInfo
                {
                    AppId = appId,
                    Name = data.GetProperty("name").GetString(),
                    HeaderImageUrl = data.GetProperty("header_image").GetString(),
                    Metadata = new Dictionary<string, object>()
                };

                // Store all metadata
                foreach (var property in data.EnumerateObject())
                {
                    if (property.Name != "screenshots") // Handle screenshots separately
                    {
                        gameInfo.Metadata[property.Name] = GetPropertyValue(property.Value);
                    }
                }

                // Extract screenshots if available
                if (data.TryGetProperty("screenshots", out JsonElement screenshots))
                {
                    foreach (var screenshot in screenshots.EnumerateArray())
                    {
                        if (screenshot.TryGetProperty("path_full", out JsonElement path))
                        {
                            gameInfo.Screenshots.Add(path.GetString());
                        }
                    }

                    // Download screenshots
                    await DownloadScreenshotsAsync(gameInfo);
                }

                // Download header image
                await DownloadHeaderImageAsync(gameInfo);

                return gameInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving Steam game info for app ID {appId}: {ex.Message}");
                return null; // Return null instead of throwing to handle API failures gracefully
            }
        }

        /// <summary>
        /// Downloads and caches the header image for a game
        /// </summary>
        private async Task DownloadHeaderImageAsync(SteamGameInfo gameInfo)
        {
            if (string.IsNullOrEmpty(gameInfo.HeaderImageUrl))
                return;

            string headerImagePath = Path.Combine(_cacheDirectory, $"{gameInfo.AppId}_header.png");

            try
            {
                // Skip if already downloaded
                if (File.Exists(headerImagePath))
                    return;

                // Download the image
                byte[] imageData = await _httpClient.GetByteArrayAsync(gameInfo.HeaderImageUrl);

                // Save to file
                await File.WriteAllBytesAsync(headerImagePath, imageData);

                Debug.WriteLine($"Downloaded header image for app ID {gameInfo.AppId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading header image for app ID {gameInfo.AppId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Downloads and caches screenshots for a game
        /// </summary>
        private async Task DownloadScreenshotsAsync(SteamGameInfo gameInfo)
        {
            for (int i = 0; i < Math.Min(gameInfo.Screenshots.Count, 5); i++) // Limit to 5 screenshots
            {
                string screenshotUrl = gameInfo.Screenshots[i];
                string screenshotPath = Path.Combine(_cacheDirectory, $"{gameInfo.AppId}_image{i + 1}.png");

                try
                {
                    // Skip if already downloaded
                    if (File.Exists(screenshotPath))
                        continue;

                    // Download the image
                    byte[] imageData = await _httpClient.GetByteArrayAsync(screenshotUrl);

                    // Save to file
                    await File.WriteAllBytesAsync(screenshotPath, imageData);

                    Debug.WriteLine($"Downloaded screenshot {i + 1} for app ID {gameInfo.AppId}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error downloading screenshot {i + 1} for app ID {gameInfo.AppId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Saves game information to the local cache
        /// </summary>
        private async Task SaveGameInfoToCacheAsync(SteamGameInfo gameInfo)
        {
            string metadataCachePath = Path.Combine(_cacheDirectory, $"{gameInfo.AppId}.bin");

            try
            {
                // Serialize the game info
                byte[] serializedData = SerializeGameInfo(gameInfo);

                // Save to file
                await File.WriteAllBytesAsync(metadataCachePath, serializedData);

                Debug.WriteLine($"Saved metadata to cache for app ID {gameInfo.AppId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving metadata to cache for app ID {gameInfo.AppId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Serializes game information to a byte array
        /// </summary>
        private byte[] SerializeGameInfo(SteamGameInfo gameInfo)
        {
            try
            {
                // Use System.Text.Json to serialize the object
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false
                };

                return JsonSerializer.SerializeToUtf8Bytes(gameInfo, options);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error serializing game info: {ex.Message}");
                return new byte[0];
            }
        }

        /// <summary>
        /// Deserializes game information from a byte array
        /// </summary>
        private SteamGameInfo DeserializeGameInfo(byte[] data)
        {
            try
            {
                // Use System.Text.Json to deserialize the object
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<SteamGameInfo>(data, options);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deserializing game info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all cached local paths for a game's images
        /// </summary>
        public List<string> GetLocalImagePaths(int appId)
        {
            var paths = new List<string>();

            try
            {
                // Add header image if it exists
                string headerImagePath = Path.Combine(_cacheDirectory, $"{appId}_header.png");
                if (File.Exists(headerImagePath))
                {
                    paths.Add(headerImagePath);
                }

                // Add all screenshot images
                int index = 1;
                while (true)
                {
                    string screenshotPath = Path.Combine(_cacheDirectory, $"{appId}_image{index}.png");
                    if (File.Exists(screenshotPath))
                    {
                        paths.Add(screenshotPath);
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting local image paths for app ID {appId}: {ex.Message}");
            }

            return paths;
        }

        /// <summary>
        /// Gets the header image path for a game
        /// </summary>
        public string GetHeaderImagePath(int appId)
        {
            try
            {
                string headerImagePath = Path.Combine(_cacheDirectory, $"{appId}_header.png");
                return File.Exists(headerImagePath) ? headerImagePath : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting header image path for app ID {appId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts a value from a JsonElement based on its type
        /// </summary>
        private object GetPropertyValue(JsonElement element)
        {
            try
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        return element.GetString();
                    case JsonValueKind.Number:
                        if (element.TryGetInt32(out int intValue))
                            return intValue;
                        if (element.TryGetInt64(out long longValue))
                            return longValue;
                        return element.GetDouble();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                    case JsonValueKind.Object:
                        var obj = new Dictionary<string, object>();
                        foreach (var property in element.EnumerateObject())
                        {
                            obj[property.Name] = GetPropertyValue(property.Value);
                        }
                        return obj;
                    case JsonValueKind.Array:
                        var array = new List<object>();
                        foreach (var item in element.EnumerateArray())
                        {
                            array.Add(GetPropertyValue(item));
                        }
                        return array;
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting property value: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the cache directory path
        /// </summary>
        public string GetCacheDirectory()
        {
            return _cacheDirectory;
        }
    }

    public class GameScanner
    {
        // Steam API key for fetching game details
        private readonly string _steamApiKey;
        private bool _hasPermission = false;
        private bool _hasCheckedPermission = false;
        private Window _window;
        private DispatcherQueue _dispatcherQueue;
        private PathsManager _pathsManager;
        private SteamApiHelper _steamApiHelper;

        // List of folder names to exclude from scanning
        private readonly string[] _excludedFolders = new[] {
            "GameSave",
            "Saves",
            "SavedGames",
            "SaveData",
            "Backups",
            "Cache",
            "Temp",
            "Logs",
            "Redistributables",
            "Redist",
            "CommonRedist",
            "__Installer",
            "_CommonRedist"
        };

        // List of common executable names that are not the main game
        private readonly string[] _ignoredExecutables = new[] {
            "unins000.exe",
            "uninstall.exe",
            "launcher.exe",
            "setup.exe",
            "install.exe",
            "updater.exe",
            "redist.exe",
            "vcredist.exe",
            "dotnetfx.exe",
            "directx.exe",
            "dxsetup.exe",
            "prerequisites.exe",
            "support.exe",
            "repair.exe",
            "crash_reporter.exe",
            "vc_redist.exe"
        };

        public event EventHandler<bool> PermissionStatusChanged;

        public GameScanner(Window window = null)
        {
            _steamApiKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");
            _window = window;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _pathsManager = new PathsManager();

            // Initialize the Steam API helper with Documents folder path
            string cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GameCenter", "Steam");
            _steamApiHelper = new SteamApiHelper(cacheDirectory);
        }

        public async Task<bool> CheckFileSystemAccessAsync()
        {
            if (_hasCheckedPermission)
                return _hasPermission;

            try
            {
                // Try to access a test folder to check permissions
                var testPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Games");

                try
                {
                    // Create the directory if it doesn't exist
                    if (!Directory.Exists(testPath))
                    {
                        Directory.CreateDirectory(testPath);
                    }

                    // Try to read the directory
                    Directory.GetDirectories(testPath);
                    _hasPermission = true;
                    Debug.WriteLine("File system access permission granted");
                }
                catch (UnauthorizedAccessException)
                {
                    // We found the directory but can't access it
                    _hasPermission = false;
                    Debug.WriteLine("File system access permission denied");
                }

                // If we still don't have permission, try to request it
                if (!_hasPermission)
                {
                    await RequestFileSystemAccessAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking file system access: {ex.Message}");
                _hasPermission = false;
            }

            _hasCheckedPermission = true;
            PermissionStatusChanged?.Invoke(this, _hasPermission);
            return _hasPermission;
        }

        private async Task RequestFileSystemAccessAsync()
        {
            try
            {
                // Use the folder picker to trigger a permission prompt
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add("*");

                // Initialize the folder picker with the window handle
                if (_window != null)
                {
                    // Get the window handle
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
                    // Associate the folder picker with the window
                    WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
                }

                // Show the folder picker
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    // User selected a folder, add it to future access list
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                    // We now have permission to at least this folder
                    _hasPermission = true;
                    Debug.WriteLine("File system access permission granted for selected folder");

                    // Add this as a custom path
                    await AddCustomPathAsync(
                        folder.Path,
                        folder.Name,
                        "Other");
                }
                else
                {
                    // User cancelled the picker
                    _hasPermission = false;
                    Debug.WriteLine("User cancelled folder picker");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error requesting file system access: {ex.Message}");
                _hasPermission = false;
            }
        }

        public async Task<Dictionary<string, List<Game>>> ScanForGamesAsync()
        {
            // Check permission first
            await CheckFileSystemAccessAsync();

            // Initialize paths manager
            await _pathsManager.InitializeAsync();

            return await Task.Run(() => ScanForGames());
        }

        public Dictionary<string, List<Game>> ScanForGames()
        {
            var gamesByLauncher = new Dictionary<string, List<Game>>
            {
                { "Steam", new List<Game>() },
                { "Xbox", new List<Game>() },
                { "Epic", new List<Game>() },
                { "Other", new List<Game>() }
            };

            try
            {
                // Only scan custom paths
                var customGames = ScanCustomPaths();

                // Group games by launcher
                foreach (var game in customGames)
                {
                    if (gamesByLauncher.ContainsKey(game.Launcher))
                    {
                        gamesByLauncher[game.Launcher].Add(game);
                    }
                    else
                    {
                        gamesByLauncher["Other"].Add(game);
                    }
                }

                // If we couldn't find any games, return sample games
                bool hasAnyGames = gamesByLauncher.Values.Any(list => list.Count > 0);
                if (!hasAnyGames && _pathsManager.CustomPaths.Count == 0)
                {
                    Debug.WriteLine("No games found, returning sample games");
                    var sampleGames = GetSampleGames();

                    // Group sample games by launcher
                    foreach (var game in sampleGames)
                    {
                        if (gamesByLauncher.ContainsKey(game.Launcher))
                        {
                            gamesByLauncher[game.Launcher].Add(game);
                        }
                        else
                        {
                            gamesByLauncher["Other"].Add(game);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning for games: {ex.Message}");
            }

            return gamesByLauncher;
        }

        private List<Game> ScanCustomPaths()
        {
            var games = new List<Game>();
            int gameId = 1000; // Start custom games at ID 1000

            // Get custom paths
            var customPaths = _pathsManager.CustomPaths;

            foreach (var customPath in customPaths)
            {
                if (!customPath.IsActive)
                    continue;

                try
                {
                    var path = customPath.Path;

                    if (!Directory.Exists(path))
                    {
                        Debug.WriteLine($"Custom path does not exist: {path}");
                        continue;
                    }

                    // Scan the entire folder structure for potential games
                    var potentialGameFolders = GetPotentialGameFolders(path);

                    foreach (var gameFolder in potentialGameFolders)
                    {
                        try
                        {
                            // Check if this is a game directory (has executable files)
                            var exeFiles = GetGameExecutables(gameFolder);

                            if (exeFiles.Count > 0)
                            {
                                // Get the main executable (usually the largest one or one with the folder name)
                                string mainExe = GetMainExecutable(exeFiles, gameFolder);

                                // Get the game name from various sources
                                string gameName = GetGameName(gameFolder, mainExe, customPath.LauncherType);

                                var game = new Game
                                {
                                    Id = gameId.ToString(),
                                    Name = gameName,
                                    Title = gameName,
                                    InstallLocation = gameFolder,
                                    Launcher = customPath.LauncherType,
                                    ExecutablePath = mainExe,
                                    LastPlayed = GetLastModifiedTime(mainExe),
                                    PlayTime = EstimatePlayTime(mainExe),
                                    Screenshots = new List<string>()
                                };

                                // Set default image based on launcher type
                                SetDefaultImageForGame(game);

                                // Set additional properties for Steam games
                                if (customPath.LauncherType.Equals("Steam", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Get the Steam AppID
                                    uint appId = GetSteamAppId(gameFolder);
                                    if (appId > 0)
                                    {
                                        game.AppId = appId;
                                        game.LaunchUri = $"steam://run/{appId}";

                                        // Fetch metadata from Steam API (async but we'll continue without waiting)
                                        Task.Run(async () => {
                                            await EnrichGameWithSteamMetadataAsync(game);
                                        });
                                    }
                                }
                                // Set launch URI for Epic games
                                else if (customPath.LauncherType.Equals("Epic", StringComparison.OrdinalIgnoreCase))
                                {
                                    string folderName = new DirectoryInfo(gameFolder).Name;
                                    game.LaunchUri = $"com.epicgames.launcher://apps/{folderName}?action=launch";
                                }
                                // Set launch URI for Xbox games
                                else if (customPath.LauncherType.Equals("Xbox", StringComparison.OrdinalIgnoreCase))
                                {
                                    string folderName = new DirectoryInfo(gameFolder).Name.Replace(" ", "");
                                    game.LaunchUri = $"ms-xbox-gamepass://{folderName}";
                                }

                                // Try to extract the icon from the executable
                                // This is done asynchronously and will update the game's ImageSource later
                                Task.Run(async () => {
                                    await ExtractAndSetIconAsync(game, mainExe, exeFiles);
                                });

                                games.Add(game);
                                gameId++;
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Debug.WriteLine($"Access denied when scanning folder: {gameFolder}");
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error scanning folder {gameFolder}: {ex.Message}");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error scanning custom path {customPath.Path}: {ex.Message}");
                }
            }

            return games;
        }

        /// <summary>
        /// Extracts and sets an icon for a game
        /// </summary>
        private async Task ExtractAndSetIconAsync(Game game, string mainExe, List<string> exeFiles)
        {
            try
            {
                // Wrap icon extraction in a try-catch to handle COM exceptions
                BitmapImage icon = null;
                try
                {
                    icon = await IconExtractor.ExtractIconFromFileAsync(mainExe);
                }
                catch (COMException comEx)
                {
                    Debug.WriteLine($"COM Exception extracting icon from {mainExe}: {comEx.Message}");
                    // Continue with alternative methods
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error extracting icon from {mainExe}: {ex.Message}");
                    // Continue with alternative methods
                }

                if (icon != null)
                {
                    // Update the game's ImageSource on the UI thread
                    _dispatcherQueue?.TryEnqueue(() => {
                        try
                        {
                            game.ImageSource = icon;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error setting icon for game {game.Name}: {ex.Message}");
                        }
                    });
                }
                else
                {
                    // If icon extraction failed, try to extract from other executables in the folder
                    bool foundIcon = false;
                    foreach (var altExe in exeFiles.Where(e => e != mainExe).Take(3))
                    {
                        try
                        {
                            icon = await IconExtractor.ExtractIconFromFileAsync(altExe);
                            if (icon != null)
                            {
                                _dispatcherQueue?.TryEnqueue(() => {
                                    try
                                    {
                                        game.ImageSource = icon;
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error setting alternative icon for game {game.Name}: {ex.Message}");
                                    }
                                });
                                foundIcon = true;
                                break;
                            }
                        }
                        catch (COMException)
                        {
                            // Silently continue to the next executable
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error extracting icon from alternative exe {altExe}: {ex.Message}");
                        }
                    }

                    // If still no icon, try to get the generic .exe icon
                    if (!foundIcon)
                    {
                        try
                        {
                            icon = await IconExtractor.ExtractIconForFileTypeAsync(".exe");
                            if (icon != null)
                            {
                                _dispatcherQueue?.TryEnqueue(() => {
                                    try
                                    {
                                        game.ImageSource = icon;
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error setting generic icon for game {game.Name}: {ex.Message}");
                                    }
                                });
                            }
                        }
                        catch (COMException)
                        {
                            // If we still get a COM exception, just use the default image
                            Debug.WriteLine($"COM Exception extracting generic icon, using default image");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error extracting generic icon: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in icon extraction process for {mainExe}: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a default image for a game based on its launcher
        /// </summary>
        private void SetDefaultImageForGame(Game game)
        {
            try
            {
                string defaultImagePath = "ms-appx:///Assets/GamePlaceholder.png";

                if (game.Launcher.Equals("Steam", StringComparison.OrdinalIgnoreCase))
                {
                    defaultImagePath = "ms-appx:///Assets/SteamPlaceholder.png";
                }
                else if (game.Launcher.Equals("Xbox", StringComparison.OrdinalIgnoreCase))
                {
                    defaultImagePath = "ms-appx:///Assets/XboxPlaceholder.png";
                }
                else if (game.Launcher.Equals("Epic", StringComparison.OrdinalIgnoreCase))
                {
                    defaultImagePath = "ms-appx:///Assets/EpicPlaceholder.png";
                }

                game.ImageUrl = defaultImagePath;

                // Also set the ImageSource with the default image
                try
                {
                    var bitmap = new BitmapImage(new Uri(defaultImagePath));
                    game.ImageSource = bitmap;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error setting default bitmap for game {game.Name}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting default image for game {game.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely loads a bitmap image from a file path
        /// </summary>
        private BitmapImage SafeLoadBitmapImage(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.UriSource = new Uri(imagePath);
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading image from {imagePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Enriches a game object with metadata from the Steam API
        /// </summary>
        private async Task EnrichGameWithSteamMetadataAsync(Game game, bool forceRefresh = false)
        {
            if (game.AppId <= 0)
                return;

            try
            {
                // Fetch metadata from Steam API
                var steamInfo = await _steamApiHelper.GetGameInfoAsync((int)game.AppId, forceRefresh);
                if (steamInfo == null)
                    return;

                // Update game properties with Steam metadata
                _dispatcherQueue?.TryEnqueue(() => {
                    try
                    {
                        // Update name if the Steam name is different and not empty
                        if (!string.IsNullOrEmpty(steamInfo.Name) &&
                            !steamInfo.Name.Equals(game.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            game.Name = steamInfo.Name;
                            game.Title = steamInfo.Name;
                        }

                        // Update description if available
                        if (steamInfo.Metadata.TryGetValue("short_description", out object descObj) &&
                            descObj is string description)
                        {
                            game.Description = description;
                        }

                        // Update developer if available
                        if (steamInfo.Metadata.TryGetValue("developers", out object devsObj) &&
                            devsObj is List<object> devs && devs.Count > 0)
                        {
                            game.Developer = devs[0].ToString();
                        }

                        // Update publisher if available
                        if (steamInfo.Metadata.TryGetValue("publishers", out object pubsObj) &&
                            pubsObj is List<object> pubs && pubs.Count > 0)
                        {
                            game.Publisher = pubs[0].ToString();
                        }

                        // Update release date if available
                        if (steamInfo.Metadata.TryGetValue("release_date", out object releaseDateObj) &&
                            releaseDateObj is Dictionary<string, object> releaseDate &&
                            releaseDate.TryGetValue("date", out object dateObj))
                        {
                            game.ReleaseDate = dateObj.ToString();
                        }

                        // Update genres if available
                        if (steamInfo.Metadata.TryGetValue("genres", out object genresObj) &&
                            genresObj is List<object> genres)
                        {
                            var genreList = new List<string>();
                            foreach (var genreObj in genres)
                            {
                                if (genreObj is Dictionary<string, object> genre &&
                                    genre.TryGetValue("description", out object genreDesc))
                                {
                                    genreList.Add(genreDesc.ToString());
                                }
                            }
                            game.Genres = string.Join(", ", genreList);
                        }

                        // Update screenshots
                        var screenshotPaths = _steamApiHelper.GetLocalImagePaths((int)game.AppId);
                        if (screenshotPaths.Count > 0)
                        {
                            game.Screenshots = screenshotPaths;
                        }

                        // Update header image
                        string headerImagePath = _steamApiHelper.GetHeaderImagePath((int)game.AppId);
                        if (!string.IsNullOrEmpty(headerImagePath))
                        {
                            game.ImageUrl = headerImagePath;

                            // Load the image as a BitmapImage
                            try
                            {
                                var bitmap = SafeLoadBitmapImage(headerImagePath);
                                if (bitmap != null)
                                {
                                    game.ImageSource = bitmap;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error loading header image for app ID {game.AppId}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating game properties for app ID {game.AppId}: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enriching game with Steam metadata for app ID {game.AppId}: {ex.Message}");
            }
        }

        private List<string> GetGameExecutables(string folderPath)
        {
            var exeFiles = new List<string>();

            try
            {
                // Get all .exe files in the directory
                var allExeFiles = Directory.GetFiles(folderPath, "*.exe", SearchOption.TopDirectoryOnly);

                // Filter out common non-game executables
                foreach (var exeFile in allExeFiles)
                {
                    var fileName = Path.GetFileName(exeFile).ToLower();

                    // Skip ignored executables
                    if (_ignoredExecutables.Any(ignored => fileName.Equals(ignored, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    // Skip very small executables (likely not the main game)
                    var fileInfo = new FileInfo(exeFile);
                    if (fileInfo.Length < 100 * 1024) // Skip files smaller than 100KB
                    {
                        continue;
                    }

                    exeFiles.Add(exeFile);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting executables from {folderPath}: {ex.Message}");
            }

            return exeFiles;
        }

        private string GetMainExecutable(List<string> exeFiles, string folderPath)
        {
            if (exeFiles.Count == 0)
                return null;

            if (exeFiles.Count == 1)
                return exeFiles[0];

            try
            {
                // Get the folder name
                string folderName = new DirectoryInfo(folderPath).Name;

                // First, look for an executable with the same name as the folder
                foreach (var exeFile in exeFiles)
                {
                    string exeName = Path.GetFileNameWithoutExtension(exeFile);

                    if (exeName.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return exeFile;
                    }
                }

                // If no match, look for executables with similar names
                foreach (var exeFile in exeFiles)
                {
                    string exeName = Path.GetFileNameWithoutExtension(exeFile);

                    if (folderName.Contains(exeName, StringComparison.OrdinalIgnoreCase) ||
                        exeName.Contains(folderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return exeFile;
                    }
                }

                // If still no match, return the largest executable
                return exeFiles.OrderByDescending(exe => new FileInfo(exe).Length).First();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error finding main executable: {ex.Message}");
                return exeFiles[0]; // Return the first one as fallback
            }
        }

        private string GetGameName(string folderPath, string exePath, string launcherType)
        {
            try
            {
                // Try different methods to get the game name

                // 1. Check for Steam appmanifest if it's a Steam game
                if (launcherType.Equals("Steam", StringComparison.OrdinalIgnoreCase))
                {
                    string steamAppName = GetSteamGameName(folderPath);
                    if (!string.IsNullOrEmpty(steamAppName))
                    {
                        return steamAppName;
                    }
                }

                // 2. Check for .ini or .cfg files that might contain the game name
                string configName = GetNameFromConfigFiles(folderPath);
                if (!string.IsNullOrEmpty(configName))
                {
                    return configName;
                }

                // 3. Get the product name from the executable's version info
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    try
                    {
                        var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                        if (!string.IsNullOrEmpty(versionInfo.ProductName))
                        {
                            return versionInfo.ProductName;
                        }
                    }
                    catch
                    {
                        // Ignore errors when getting version info
                    }
                }

                // 4. Use the folder name as a fallback, with some cleanup
                string folderName = new DirectoryInfo(folderPath).Name;

                // Clean up the folder name
                folderName = folderName.Replace("_", " ").Replace("-", " ");

                // Remove version numbers
                folderName = Regex.Replace(folderName, @"\s*v?[\d\.]+\s*$", "");

                // Add spaces before capital letters for CamelCase names
                folderName = Regex.Replace(folderName, @"([a-z])([A-Z])", "$1 $2");

                return folderName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting game name: {ex.Message}");
                return new DirectoryInfo(folderPath).Name; // Return the folder name as fallback
            }
        }

        private string GetSteamGameName(string gamePath)
        {
            try
            {
                // Try to find the Steam installation directory
                string steamPath = null;

                // Check if the game path contains "steamapps/common"
                var match = Regex.Match(gamePath, @"(.*?)[\\\/]steamapps[\\\/]common[\\\/]", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    steamPath = match.Groups[1].Value;
                }

                if (string.IsNullOrEmpty(steamPath))
                    return null;

                // Get the game folder name
                string gameFolder = new DirectoryInfo(gamePath).Name;

                // Look for appmanifest files in the steamapps directory
                string manifestsDir = Path.Combine(steamPath, "steamapps");
                if (!Directory.Exists(manifestsDir))
                    return null;

                var manifestFiles = Directory.GetFiles(manifestsDir, "appmanifest_*.acf");
                foreach (var manifestFile in manifestFiles)
                {
                    // Read the manifest file
                    string content = File.ReadAllText(manifestFile);

                    // Check if this manifest is for our game
                    if (content.Contains($"\"installdir\"\t\t\"{gameFolder}\"") ||
                        content.Contains($"\"installdir\"\t\t\"{gameFolder.ToLower()}\""))
                    {
                        // Extract the game name
                        var nameMatch = Regex.Match(content, "\"name\"\\s+\"([^\"]+)\"");
                        if (nameMatch.Success)
                        {
                            return nameMatch.Groups[1].Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting Steam game name: {ex.Message}");
            }

            return null;
        }

        private uint GetSteamAppId(string gamePath)
        {
            try
            {
                // Try to find the Steam installation directory
                string steamPath = null;

                // Check if the game path contains "steamapps/common"
                var match = Regex.Match(gamePath, @"(.*?)[\\\/]steamapps[\\\/]common[\\\/]", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    steamPath = match.Groups[1].Value;
                }

                if (string.IsNullOrEmpty(steamPath))
                    return 0;

                // Get the game folder name
                string gameFolder = new DirectoryInfo(gamePath).Name;

                // Look for appmanifest files in the steamapps directory
                string manifestsDir = Path.Combine(steamPath, "steamapps");
                if (!Directory.Exists(manifestsDir))
                    return 0;

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
                            return uint.Parse(appIdMatch.Groups[1].Value);
                        }
                    }
                }

                // If we couldn't find the AppID in the manifest, try to extract it from the filename
                foreach (var manifestFile in manifestFiles)
                {
                    var fileNameMatch = Regex.Match(Path.GetFileName(manifestFile), @"appmanifest_(\d+)\.acf");
                    if (fileNameMatch.Success)
                    {
                        // Check if this manifest is for our game
                        string content = File.ReadAllText(manifestFile);
                        if (content.Contains($"\"installdir\"\t\t\"{gameFolder}\"") ||
                            content.Contains($"\"installdir\"\t\t\"{gameFolder.ToLower()}\""))
                        {
                            return uint.Parse(fileNameMatch.Groups[1].Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting Steam AppID: {ex.Message}");
            }

            return 0;
        }

        private string GetNameFromConfigFiles(string folderPath)
        {
            try
            {
                // Look for common config files
                var configFiles = new List<string>();

                // Add .ini files
                configFiles.AddRange(Directory.GetFiles(folderPath, "*.ini", SearchOption.TopDirectoryOnly));

                // Add .cfg files
                configFiles.AddRange(Directory.GetFiles(folderPath, "*.cfg", SearchOption.TopDirectoryOnly));

                // Add .xml files
                configFiles.AddRange(Directory.GetFiles(folderPath, "*.xml", SearchOption.TopDirectoryOnly));

                // Check each config file for a game name
                foreach (var configFile in configFiles)
                {
                    try
                    {
                        string content = File.ReadAllText(configFile);

                        // Look for common patterns that might indicate a game name
                        var nameMatches = new List<string>();

                        // Pattern: "GameName" = "Value"
                        nameMatches.AddRange(FindMatches(content, "\"GameName\"\\s*=\\s*\"([^\"]+)\""));

                        // Pattern: <GameName>Value</GameName>
                        nameMatches.AddRange(FindMatches(content, "<GameName>([^<]+)</GameName>"));

                        // Pattern: "Name" = "Value"
                        nameMatches.AddRange(FindMatches(content, "\"Name\"\\s*=\\s*\"([^\"]+)\""));

                        // Pattern: <Name>Value</Name>
                        nameMatches.AddRange(FindMatches(content, "<Name>([^<]+)</Name>"));

                        // Pattern: "Title" = "Value"
                        nameMatches.AddRange(FindMatches(content, "\"Title\"\\s*=\\s*\"([^\"]+)\""));

                        // Pattern: <Title>Value</Title>
                        nameMatches.AddRange(FindMatches(content, "<Title>([^<]+)</Title>"));

                        // Return the first non-empty match
                        foreach (var match in nameMatches)
                        {
                            if (!string.IsNullOrWhiteSpace(match))
                            {
                                return match;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors when reading config files
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting name from config files: {ex.Message}");
            }

            return null;
        }

        private List<string> FindMatches(string content, string pattern)
        {
            var result = new List<string>();

            try
            {
                var matches = Regex.Matches(content, pattern);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        result.Add(match.Groups[1].Value);
                    }
                }
            }
            catch
            {
                // Ignore regex errors
            }

            return result;
        }

        private List<string> GetPotentialGameFolders(string rootPath)
        {
            var result = new List<string>();

            try
            {
                // Add the root path itself as a potential game folder
                result.Add(rootPath);

                // Get all immediate subdirectories
                var subDirs = Directory.GetDirectories(rootPath);

                foreach (var dir in subDirs)
                {
                    // Skip excluded folders
                    var dirName = new DirectoryInfo(dir).Name;
                    if (_excludedFolders.Contains(dirName, StringComparer.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Skipping excluded folder: {dir}");
                        continue;
                    }

                    // Add this directory as a potential game folder
                    result.Add(dir);

                    try
                    {
                        // Recursively scan subdirectories
                        var nestedFolders = GetPotentialGameFolders(dir);
                        result.AddRange(nestedFolders);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Debug.WriteLine($"Access denied when scanning subdirectories of {dir}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error scanning subdirectories of {dir}: {ex.Message}");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine($"Access denied when getting directories in {rootPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting directories in {rootPath}: {ex.Message}");
            }

            return result;
        }

        private List<Game> GetSampleGames()
        {
            // Return some sample games when we don't have permission
            // This is just for demonstration purposes
            var games = new List<Game>();

            games.Add(new Game
            {
                Id = "1",
                GameId = 1,
                Name = "Sample Steam Game",
                Title = "Sample Steam Game",
                InstallLocation = @"C:\Program Files (x86)\Steam\steamapps\common\SampleGame",
                Launcher = "Steam",
                LastPlayed = DateTime.Now.AddDays(-2),
                PlayTime = 120,
                ImageUrl = "ms-appx:///Assets/SteamPlaceholder.png",
                Screenshots = new List<string>()
            });

            games.Add(new Game
            {
                Id = "2",
                GameId = 2,
                Name = "Sample Xbox Game",
                Title = "Sample Xbox Game",
                InstallLocation = @"C:\XboxGames\SampleXboxGame",
                Launcher = "Xbox",
                LastPlayed = DateTime.Now.AddDays(-5),
                PlayTime = 60,
                ImageUrl = "ms-appx:///Assets/XboxPlaceholder.png",
                Screenshots = new List<string>()
            });

            games.Add(new Game
            {
                Id = "3",
                GameId = 3,
                Name = "Sample Epic Game",
                Title = "Sample Epic Game",
                InstallLocation = @"C:\Program Files\Epic Games\SampleEpicGame",
                Launcher = "Epic",
                LastPlayed = DateTime.Now.AddDays(-10),
                PlayTime = 30,
                ImageUrl = "ms-appx:///Assets/EpicPlaceholder.png",
                Screenshots = new List<string>()
            });

            games.Add(new Game
            {
                Id = "4",
                GameId = 4,
                Name = "Sample Other Game",
                Title = "Sample Other Game",
                InstallLocation = @"C:\Games\SampleOtherGame",
                Launcher = "Other",
                LastPlayed = DateTime.Now.AddDays(-15),
                PlayTime = 45,
                ImageUrl = "ms-appx:///Assets/GamePlaceholder.png",
                Screenshots = new List<string>()
            });

            return games;
        }

        private DateTime GetLastModifiedTime(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return DateTime.Now.AddDays(-30); // Default to 30 days ago

                return File.GetLastWriteTime(filePath);
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine($"Access denied when getting last modified time for {filePath}");
                return DateTime.Now.AddDays(-30); // Default to 30 days ago if can't access
            }
            catch
            {
                return DateTime.Now.AddDays(-30); // Default to 30 days ago if can't determine
            }
        }

        private int EstimatePlayTime(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return 0;

            try
            {
                // This is just a rough estimate based on file access times
                var fileInfo = new FileInfo(filePath);
                var creationTime = fileInfo.CreationTime;
                var lastAccessTime = fileInfo.LastAccessTime;

                // If the file was accessed after creation, estimate some playtime
                if (lastAccessTime > creationTime)
                {
                    var daysSinceCreation = (DateTime.Now - creationTime).TotalDays;

                    // Rough estimate: 1 hour per week since creation, capped at 100 hours
                    return Math.Min(100, (int)(daysSinceCreation / 7));
                }

                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine($"Access denied when estimating play time for {filePath}");
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        // Add this property to expose permission status
        public bool HasPermission => _hasPermission;

        // Add this property to expose the paths manager
        public PathsManager PathsManager => _pathsManager;

        // Add method to add a custom path
        public async Task AddCustomPathAsync(string path, string name, string launcherType)
        {
            var gamePath = new GamePath
            {
                Path = path,
                Name = name,
                LauncherType = launcherType
            };

            await _pathsManager.AddPathAsync(gamePath);
        }

        // Add method to remove a custom path
        public async Task RemoveCustomPathAsync(string pathId)
        {
            await _pathsManager.RemovePathAsync(pathId);
        }

        // Add method to get custom paths
        public async Task<List<GamePath>> GetCustomPathsAsync()
        {
            return await _pathsManager.GetPathsAsync();
        }

        // Add method to refresh Steam metadata for a specific game
        public async Task RefreshSteamMetadataAsync(Game game)
        {
            if (game == null || game.AppId <= 0 || !game.Launcher.Equals("Steam", StringComparison.OrdinalIgnoreCase))
                return;

            await EnrichGameWithSteamMetadataAsync(game, true);
        }

        // Get the cache directory path
        public string GetCacheDirectory()
        {
            return _steamApiHelper.GetCacheDirectory();
        }
    }
}
