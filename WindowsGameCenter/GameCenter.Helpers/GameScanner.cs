using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using GameCenter.Helpers.Models;

namespace GameCenter.Helpers
{
    public class GameScanner
    {
        // Steam API key for fetching game details
        private readonly string _steamApiKey;
        private bool _hasPermission = false;
        private bool _hasCheckedPermission = false;
        private Window _window;
        private DispatcherQueue _dispatcherQueue;
        private PathsManager _pathsManager;

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
                    System.Diagnostics.Debug.WriteLine("File system access permission granted");
                }
                catch (UnauthorizedAccessException)
                {
                    // We found the directory but can't access it
                    _hasPermission = false;
                    System.Diagnostics.Debug.WriteLine("File system access permission denied");
                }

                // If we still don't have permission, try to request it
                if (!_hasPermission)
                {
                    await RequestFileSystemAccessAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking file system access: {ex.Message}");
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
                    System.Diagnostics.Debug.WriteLine("File system access permission granted for selected folder");

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
                    System.Diagnostics.Debug.WriteLine("User cancelled folder picker");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error requesting file system access: {ex.Message}");
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
                    System.Diagnostics.Debug.WriteLine("No games found, returning sample games");
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
                System.Diagnostics.Debug.WriteLine($"Error scanning for games: {ex.Message}");
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
                        System.Diagnostics.Debug.WriteLine($"Custom path does not exist: {path}");
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

                                // Set additional properties for Steam games
                                if (customPath.LauncherType.Equals("Steam", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Get the Steam AppID
                                    uint appId = GetSteamAppId(gameFolder);
                                    if (appId > 0)
                                    {
                                        game.AppId = appId;
                                        game.LaunchUri = $"steam://run/{appId}";
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

                                // Set image based on launcher type (will be replaced with actual icon if available)
                                switch (customPath.LauncherType.ToLower())
                                {
                                    case "steam":
                                        game.ImageUrl = "ms-appx:///Assets/SteamPlaceholder.png";
                                        break;
                                    case "xbox":
                                        game.ImageUrl = "ms-appx:///Assets/XboxPlaceholder.png";
                                        break;
                                    case "epic":
                                        game.ImageUrl = "ms-appx:///Assets/EpicPlaceholder.png";
                                        break;
                                    default:
                                        game.ImageUrl = "ms-appx:///Assets/GamePlaceholder.png";
                                        break;
                                }

                                // Try to extract the icon from the executable
                                // This is done asynchronously and will update the game's ImageSource later
                                Task.Run(async () => {
                                    try
                                    {
                                        var icon = await IconExtractor.ExtractIconFromFileAsync(mainExe);
                                        if (icon != null)
                                        {
                                            // Update the game's ImageSource on the UI thread
                                            _dispatcherQueue?.TryEnqueue(() => {
                                                game.ImageSource = icon;
                                            });
                                        }
                                        else
                                        {
                                            // If icon extraction failed, try to extract from other executables in the folder
                                            bool foundIcon = false;
                                            foreach (var altExe in exeFiles.Where(e => e != mainExe).Take(3))
                                            {
                                                icon = await IconExtractor.ExtractIconFromFileAsync(altExe);
                                                if (icon != null)
                                                {
                                                    _dispatcherQueue?.TryEnqueue(() => {
                                                        game.ImageSource = icon;
                                                    });
                                                    foundIcon = true;
                                                    break;
                                                }
                                            }

                                            // If still no icon, try to get the generic .exe icon
                                            if (!foundIcon)
                                            {
                                                icon = await IconExtractor.ExtractIconForFileTypeAsync(".exe");
                                                if (icon != null)
                                                {
                                                    _dispatcherQueue?.TryEnqueue(() => {
                                                        game.ImageSource = icon;
                                                    });
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error extracting icon from {mainExe}: {ex.Message}");

                                        // Try to extract from other executables in the folder
                                        bool foundIcon = false;
                                        foreach (var altExe in exeFiles.Where(e => e != mainExe).Take(3))
                                        {
                                            try
                                            {
                                                var icon = await IconExtractor.ExtractIconFromFileAsync(altExe);
                                                if (icon != null)
                                                {
                                                    _dispatcherQueue?.TryEnqueue(() => {
                                                        game.ImageSource = icon;
                                                    });
                                                    foundIcon = true;
                                                    break;
                                                }
                                            }
                                            catch
                                            {
                                                // Ignore errors for alternative executables
                                            }
                                        }

                                        // If still no icon, try to get the generic .exe icon
                                        if (!foundIcon)
                                        {
                                            try
                                            {
                                                var icon = await IconExtractor.ExtractIconForFileTypeAsync(".exe");
                                                if (icon != null)
                                                {
                                                    _dispatcherQueue?.TryEnqueue(() => {
                                                        game.ImageSource = icon;
                                                    });
                                                }
                                            }
                                            catch
                                            {
                                                // Ignore errors for fallback icon
                                            }
                                        }
                                    }
                                });

                                games.Add(game);
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            System.Diagnostics.Debug.WriteLine($"Access denied when scanning folder: {gameFolder}");
                            continue;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error scanning folder {gameFolder}: {ex.Message}");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error scanning custom path {customPath.Path}: {ex.Message}");
                }
            }

            return games;
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
                System.Diagnostics.Debug.WriteLine($"Error getting executables from {folderPath}: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error finding main executable: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error getting game name: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error getting Steam game name: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error getting Steam AppID: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error getting name from config files: {ex.Message}");
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
                        System.Diagnostics.Debug.WriteLine($"Skipping excluded folder: {dir}");
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
                        System.Diagnostics.Debug.WriteLine($"Access denied when scanning subdirectories of {dir}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error scanning subdirectories of {dir}: {ex.Message}");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine($"Access denied when getting directories in {rootPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting directories in {rootPath}: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Access denied when getting last modified time for {filePath}");
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
                System.Diagnostics.Debug.WriteLine($"Access denied when estimating play time for {filePath}");
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
    }
}

