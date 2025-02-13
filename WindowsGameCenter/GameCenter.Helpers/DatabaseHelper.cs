using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace GameCenter.Helpers
{
    public class DatabaseHelper
    {
        private string _gamesDbPath;
        private string _userDbPath;
        private bool _isInitialized = false;
        private readonly ConcurrentDictionary<int, Game> _gameCache = new ConcurrentDictionary<int, Game>();

        public DatabaseHelper()
        {
            InitializeAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                StorageFolder documentsFolder = await KnownFolders.DocumentsLibrary.CreateFolderAsync("GameCenter", CreationCollisionOption.OpenIfExists);
                _gamesDbPath = Path.Combine(documentsFolder.Path, "gamesmetadata.db");
                _userDbPath = Path.Combine(documentsFolder.Path, "usermetadata.db");

                Debug.WriteLine($"Games DB Path: {_gamesDbPath}");
                Debug.WriteLine($"User DB Path: {_userDbPath}");

                await Task.Run(() =>
                {
                    if (!File.Exists(_gamesDbPath))
                    {
                        Debug.WriteLine("Games database does not exist. Creating and populating...");
                        CreateGamesDatabase();
                        InsertGameData();
                    }
                    else
                    {
                        Debug.WriteLine("Games database already exists.");
                    }

                    if (!File.Exists(_userDbPath))
                    {
                        Debug.WriteLine("User database does not exist. Creating...");
                        CreateUserDatabase();
                    }
                    else
                    {
                        Debug.WriteLine("User database already exists.");
                    }
                });

                _isInitialized = true;
                Debug.WriteLine("Databases initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing databases: {ex.Message}");
                throw;
            }
        }

        private void CreateGamesDatabase()
        {
            using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
            {
                db.Open();

                string tableCommand = """
                    CREATE TABLE IF NOT EXISTS Games (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT,
                        ImageUrl TEXT,
                        LaunchUri TEXT,
                        Launcher TEXT,
                        LastPlayed TEXT,
                        Screenshots TEXT,
                        ExecutablePath TEXT,
                        InstallLocation TEXT,
                        Version TEXT,
                        Publisher TEXT,
                        Developer TEXT,
                        ReleaseDate TEXT,
                        Genre TEXT,
                        PlayTime INTEGER
                    )
                """;

                var createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteNonQuery();
            }
        }

        private void CreateUserDatabase()
        {
            using (var db = new SqliteConnection($"Filename={_userDbPath}"))
            {
                db.Open();

                string tableCommand = """
                    CREATE TABLE IF NOT EXISTS UserPreferences (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId TEXT NOT NULL,
                        PreferenceKey TEXT NOT NULL,
                        PreferenceValue TEXT
                    )
                """;

                var createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteNonQuery();
            }
        }

        private void InsertGameData()
        {
            var games = new List<Game>
            {
                new Game
                {
                    Title = "Cyberpunk 2077",
                    Description = "Cyberpunk 2077 is an open-world, action-adventure RPG set in Night City, a megalopolis obsessed with power, glamour and body modification.",
                    ImageUrl = "ms-appx:///Assets/Games/Cyberpunk2077/cover.jpg",
                    LaunchUri = "steam://rungameid/1091500",
                    Launcher = "Steam",
                    LastPlayed = DateTime.Now.AddDays(-2),
                    Screenshots = new List<string> {
                        "ms-appx:///Assets/Games/Cyberpunk2077/screenshot1.jpg",
                        "ms-appx:///Assets/Games/Cyberpunk2077/screenshot2.jpg"
                    },
                    ExecutablePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Cyberpunk 2077\\bin\\x64\\Cyberpunk2077.exe",
                    InstallLocation = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Cyberpunk 2077",
                    Version = "2.0",
                    Publisher = "CD PROJEKT RED",
                    Developer = "CD PROJEKT RED",
                    ReleaseDate = "2020-12-10",
                    Genre = "RPG, Action, Open World",
                    PlayTime = 120
                },
                new Game
                {
                    Title = "Red Dead Redemption 2",
                    Description = "Red Dead Redemption 2 is an epic tale of life in America's unforgiving heartland. The game's vast and atmospheric world also provides the foundation for a brand new online multiplayer experience.",
                    ImageUrl = "ms-appx:///Assets/Games/RDR2/cover.jpg",
                    LaunchUri = "steam://rungameid/1174180",
                    Launcher = "Steam",
                    LastPlayed = DateTime.Now.AddDays(-1),
                    Screenshots = new List<string> {
                        "ms-appx:///Assets/Games/RDR2/screenshot1.jpg",
                        "ms-appx:///Assets/Games/RDR2/screenshot2.jpg"
                    },
                    ExecutablePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Red Dead Redemption 2\\RDR2.exe",
                    InstallLocation = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Red Dead Redemption 2",
                    Version = "1.0.1436.28",
                    Publisher = "Rockstar Games",
                    Developer = "Rockstar Games",
                    ReleaseDate = "2019-12-05",
                    Genre = "Action, Adventure, Open World",
                    PlayTime = 85
                },
                new Game
                {
                    Title = "The Witcher 3: Wild Hunt",
                    Description = "You are Geralt of Rivia, mercenary monster slayer. Before you stands a war-torn, monster-infested continent you can explore at will. Your current contract? Tracking down Ciri — the Child of Prophecy, a living weapon that can alter the shape of the world.",
                    ImageUrl = "ms-appx:///Assets/Games/Witcher3/cover.jpg",
                    LaunchUri = "steam://rungameid/292030",
                    Launcher = "Steam",
                    LastPlayed = DateTime.Now.AddDays(-5),
                    Screenshots = new List<string> {
                        "ms-appx:///Assets/Games/Witcher3/screenshot1.jpg",
                        "ms-appx:///Assets/Games/Witcher3/screenshot2.jpg"
                    },
                    ExecutablePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\The Witcher 3\\bin\\x64\\witcher3.exe",
                    InstallLocation = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\The Witcher 3",
                    Version = "4.0",
                    Publisher = "CD PROJEKT RED",
                    Developer = "CD PROJEKT RED",
                    ReleaseDate = "2015-05-18",
                    Genre = "RPG, Action, Open World",
                    PlayTime = 250
                },
                new Game
                {
                    Title = "Baldur's Gate 3",
                    Description = "Gather your party, and return to the Forgotten Realms in a tale of fellowship and betrayal, sacrifice and survival, and the lure of absolute power.",
                    ImageUrl = "ms-appx:///Assets/Games/BG3/cover.jpg",
                    LaunchUri = "steam://rungameid/1086940",
                    Launcher = "Steam",
                    LastPlayed = DateTime.Now.AddDays(-3),
                    Screenshots = new List<string> {
                        "ms-appx:///Assets/Games/BG3/screenshot1.jpg",
                        "ms-appx:///Assets/Games/BG3/screenshot2.jpg"
                    },
                    ExecutablePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Baldurs Gate 3\\bin\\bg3.exe",
                    InstallLocation = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Baldurs Gate 3",
                    Version = "4.1.1.3635036",
                    Publisher = "Larian Studios",
                    Developer = "Larian Studios",
                    ReleaseDate = "2023-08-03",
                    Genre = "RPG, Turn-Based, Fantasy",
                    PlayTime = 160
                }
            };

            using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
            {
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    foreach (var game in games)
                    {
                        var checkCommand = new SqliteCommand(
                            "SELECT COUNT(*) FROM Games WHERE Title = @title", db, transaction);
                        checkCommand.Parameters.AddWithValue("@title", game.Title);
                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (count == 0)
                        {
                            var insertCommand = new SqliteCommand(
                                """
                                INSERT INTO Games (
                                    Title, Description, ImageUrl, LaunchUri, Launcher, LastPlayed, 
                                    Screenshots, ExecutablePath, InstallLocation, Version, 
                                    Publisher, Developer, ReleaseDate, Genre, PlayTime
                                )
                                VALUES (
                                    @title, @desc, @img, @uri, @launcher, @lastPlayed, 
                                    @screenshots, @execPath, @installLoc, @version,
                                    @publisher, @developer, @releaseDate, @genre, @playTime
                                )
                                """, db, transaction);

                            insertCommand.Parameters.AddWithValue("@title", game.Title);
                            insertCommand.Parameters.AddWithValue("@desc", game.Description);
                            insertCommand.Parameters.AddWithValue("@img", game.ImageUrl);
                            insertCommand.Parameters.AddWithValue("@uri", game.LaunchUri);
                            insertCommand.Parameters.AddWithValue("@launcher", game.Launcher);
                            insertCommand.Parameters.AddWithValue("@lastPlayed", game.LastPlayed.ToString("O"));
                            insertCommand.Parameters.AddWithValue("@screenshots", string.Join(";", game.Screenshots ?? new List<string>()));
                            insertCommand.Parameters.AddWithValue("@execPath", game.ExecutablePath);
                            insertCommand.Parameters.AddWithValue("@installLoc", game.InstallLocation);
                            insertCommand.Parameters.AddWithValue("@version", game.Version);
                            insertCommand.Parameters.AddWithValue("@publisher", game.Publisher);
                            insertCommand.Parameters.AddWithValue("@developer", game.Developer);
                            insertCommand.Parameters.AddWithValue("@releaseDate", game.ReleaseDate);
                            insertCommand.Parameters.AddWithValue("@genre", game.Genre);
                            insertCommand.Parameters.AddWithValue("@playTime", game.PlayTime);

                            insertCommand.ExecuteNonQuery();
                            Debug.WriteLine($"Inserted game: {game.Title}");
                        }
                        else
                        {
                            Debug.WriteLine($"Game already exists: {game.Title}");
                        }
                    }
                    transaction.Commit();
                }
                Debug.WriteLine($"Finished checking/inserting games");
            }
        }

        public List<Game> LoadGames()
        {
            var games = new List<Game>();

            try
            {
                Debug.WriteLine($"Attempting to load games from: {_gamesDbPath}");

                if (!File.Exists(_gamesDbPath))
                {
                    Debug.WriteLine($"Database file does not exist at: {_gamesDbPath}");
                    return games;
                }

                using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
                {
                    db.Open();
                    Debug.WriteLine("Database opened successfully");

                    var selectCommand = new SqliteCommand(
                        "SELECT * FROM Games ORDER BY LastPlayed DESC", db);

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                var gameId = reader.GetInt32(0);
                                if (_gameCache.TryGetValue(gameId, out var cachedGame))
                                {
                                    games.Add(cachedGame);
                                }
                                else
                                {
                                    var game = new Game
                                    {
                                        Id = gameId,
                                        Title = reader.GetString(1),
                                        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                        ImageUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        LaunchUri = reader.IsDBNull(4) ? null : reader.GetString(4),
                                        Launcher = reader.IsDBNull(5) ? null : reader.GetString(5),
                                        LastPlayed = reader.IsDBNull(6) ? DateTime.MinValue : DateTime.Parse(reader.GetString(6)),
                                        Screenshots = reader.IsDBNull(7) ? new List<string>() : new List<string>(reader.GetString(7).Split(';', StringSplitOptions.RemoveEmptyEntries)),
                                        ExecutablePath = reader.IsDBNull(8) ? null : reader.GetString(8),
                                        InstallLocation = reader.IsDBNull(9) ? null : reader.GetString(9),
                                        Version = reader.IsDBNull(10) ? null : reader.GetString(10),
                                        Publisher = reader.IsDBNull(11) ? null : reader.GetString(11),
                                        Developer = reader.IsDBNull(12) ? null : reader.GetString(12),
                                        ReleaseDate = reader.IsDBNull(13) ? null : reader.GetString(13),
                                        Genre = reader.IsDBNull(14) ? null : reader.GetString(14),
                                        PlayTime = reader.IsDBNull(15) ? 0 : reader.GetInt32(15),
                                        ImageSource = null,
                                        ScreenshotSources = new List<Microsoft.UI.Xaml.Media.Imaging.BitmapImage>()
                                    };
                                    games.Add(game);
                                    _gameCache.TryAdd(gameId, game);
                                }
                                Debug.WriteLine($"Loaded game: {games[games.Count - 1].Title}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error loading individual game: {ex.Message}");
                            }
                        }
                    }
                }

                Debug.WriteLine($"Successfully loaded {games.Count} games");
            }
            catch (SqliteException ex)
            {
                Debug.WriteLine($"SQLite error loading games: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error loading games: {ex.Message}");
            }

            return games;
        }

        public void UpdateGame(Game game)
        {
            using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
            {
                db.Open();

                var updateCommand = new SqliteCommand(
                    """
                    UPDATE Games 
                    SET LastPlayed = @lastPlayed,
                        PlayTime = @playTime
                    WHERE Id = @id
                    """, db);

                updateCommand.Parameters.AddWithValue("@lastPlayed", game.LastPlayed.ToString("O"));
                updateCommand.Parameters.AddWithValue("@playTime", game.PlayTime);
                updateCommand.Parameters.AddWithValue("@id", game.Id);

                updateCommand.ExecuteNonQuery();

                // Update the cache
                _gameCache[game.Id] = game;
            }
        }

        public void DeleteGame(int id)
        {
            using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
            {
                db.Open();

                var deleteCommand = new SqliteCommand(
                    "DELETE FROM Games WHERE Id = @id", db);

                deleteCommand.Parameters.AddWithValue("@id", id);
                deleteCommand.ExecuteNonQuery();

                // Remove from cache
                _gameCache.TryRemove(id, out _);
            }
        }

        public void SaveUserPreference(string userId, string key, string value)
        {
            using (var db = new SqliteConnection($"Filename={_userDbPath}"))
            {
                db.Open();

                var upsertCommand = new SqliteCommand(
                    """
                    INSERT OR REPLACE INTO UserPreferences (UserId, PreferenceKey, PreferenceValue)
                    VALUES (@userId, @key, @value)
                    """, db);

                upsertCommand.Parameters.AddWithValue("@userId", userId);
                upsertCommand.Parameters.AddWithValue("@key", key);
                upsertCommand.Parameters.AddWithValue("@value", value);

                upsertCommand.ExecuteNonQuery();
            }
        }

        public string GetUserPreference(string userId, string key)
        {
            using (var db = new SqliteConnection($"Filename={_userDbPath}"))
            {
                db.Open();

                var selectCommand = new SqliteCommand(
                    "SELECT PreferenceValue FROM UserPreferences WHERE UserId = @userId AND PreferenceKey = @key", db);

                selectCommand.Parameters.AddWithValue("@userId", userId);
                selectCommand.Parameters.AddWithValue("@key", key);

                var result = selectCommand.ExecuteScalar();
                return result?.ToString();
            }
        }
    }
}