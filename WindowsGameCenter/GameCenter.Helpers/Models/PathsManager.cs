using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace GameCenter.Helpers.Models
{
    public class PathsManager
    {
        private const string PathsFileName = "paths.json";
        private List<GamePath> _customPaths = new List<GamePath>();
        private bool _isInitialized = false;

        public List<GamePath> CustomPaths => _customPaths;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                // Get the local folder
                var localFolder = ApplicationData.Current.LocalFolder;
                var pathsFile = await localFolder.TryGetItemAsync(PathsFileName) as StorageFile;

                if (pathsFile != null)
                {
                    // Read the file
                    var json = await FileIO.ReadTextAsync(pathsFile);

                    // Deserialize the paths
                    var paths = JsonSerializer.Deserialize<List<GamePath>>(json);

                    if (paths != null)
                    {
                        _customPaths = paths;
                        System.Diagnostics.Debug.WriteLine($"Loaded {_customPaths.Count} custom paths");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No custom paths file found, creating a new one");
                    _customPaths = new List<GamePath>();
                    await SavePathsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading custom paths: {ex.Message}");
                _customPaths = new List<GamePath>();
            }

            _isInitialized = true;
        }

        public async Task SavePathsAsync()
        {
            try
            {
                // Get the local folder
                var localFolder = ApplicationData.Current.LocalFolder;
                var pathsFile = await localFolder.CreateFileAsync(PathsFileName, CreationCollisionOption.ReplaceExisting);

                // Serialize the paths
                var json = JsonSerializer.Serialize(_customPaths, new JsonSerializerOptions { WriteIndented = true });

                // Write to the file
                await FileIO.WriteTextAsync(pathsFile, json);

                System.Diagnostics.Debug.WriteLine($"Saved {_customPaths.Count} custom paths");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving custom paths: {ex.Message}");
            }
        }

        public async Task AddPathAsync(GamePath path)
        {
            await InitializeAsync();

            // Check if the path already exists
            var existingPath = _customPaths.Find(p => p.Path.Equals(path.Path, StringComparison.OrdinalIgnoreCase));

            if (existingPath != null)
            {
                // Update the existing path
                existingPath.Name = path.Name;
                existingPath.LauncherType = path.LauncherType;
                existingPath.IsActive = true;
            }
            else
            {
                // Add the new path
                _customPaths.Add(path);
            }

            await SavePathsAsync();
        }

        public async Task RemovePathAsync(string pathId)
        {
            await InitializeAsync();

            // Find the path
            var pathIndex = _customPaths.FindIndex(p => p.Id == pathId);

            if (pathIndex >= 0)
            {
                // Remove the path
                _customPaths.RemoveAt(pathIndex);
                await SavePathsAsync();
            }
        }

        public async Task<List<GamePath>> GetPathsAsync()
        {
            await InitializeAsync();
            return _customPaths;
        }
    }
}
