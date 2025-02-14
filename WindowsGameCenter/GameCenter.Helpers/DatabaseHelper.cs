using GameCenter.Commun;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace GameCenter.Helpers
{
    public class DatabaseHelper
    {
        private static string _userDbPath;
        private static string _gamesDbPath;

        public static async Task InitializeAsync()
        {
            StorageFolder documentsFolder = await KnownFolders.DocumentsLibrary.CreateFolderAsync("GameCenter", CreationCollisionOption.OpenIfExists);
            _userDbPath = System.IO.Path.Combine(documentsFolder.Path, "usermetadata.db");
            _gamesDbPath = System.IO.Path.Combine(documentsFolder.Path, "gamesmetadata.db");

            CreateUserDatabase();
            CreateGamesDatabase();
        }

        private static void CreateUserDatabase()
        {
            using (var db = new SqliteConnection($"Filename={_userDbPath}"))
            {
                db.Open();

                string tableCommand = """
                    CREATE TABLE IF NOT EXISTS UserData (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SteamId TEXT NOT NULL UNIQUE,
                        PersonaName TEXT,
                        ProfileUrl TEXT,
                        AvatarUrl TEXT,
                        AccessToken TEXT,
                        RefreshToken TEXT,
                        TokenExpirationTime TEXT
                    )
                """;

                var createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteNonQuery();
            }
        }

        private static void CreateGamesDatabase()
        {
            using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
            {
                db.Open();

                string tableCommand = """
                    CREATE TABLE IF NOT EXISTS Games (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AppId INTEGER NOT NULL,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        ImageUrl TEXT,
                        PlaytimeForever INTEGER,
                        LastPlayed TEXT
                    )
                """;

                var createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteNonQuery();
            }
        }

        public static async Task SaveUserDataAsync(UserData userData)
        {
            using (var db = new SqliteConnection($"Filename={_userDbPath}"))
            {
                await db.OpenAsync();

                var insertCommand = new SqliteCommand(
                    """
                    INSERT OR REPLACE INTO UserData (
                        SteamId, PersonaName, ProfileUrl, AvatarUrl, AccessToken, RefreshToken, TokenExpirationTime
                    ) VALUES (
                        @steamId, @personaName, @profileUrl, @avatarUrl, @accessToken, @refreshToken, @tokenExpirationTime
                    )
                    """, db);

                insertCommand.Parameters.AddWithValue("@steamId", userData.SteamId);
                insertCommand.Parameters.AddWithValue("@personaName", userData.PersonaName);
                insertCommand.Parameters.AddWithValue("@profileUrl", userData.ProfileUrl);
                insertCommand.Parameters.AddWithValue("@avatarUrl", userData.AvatarUrl);
                insertCommand.Parameters.AddWithValue("@accessToken", userData.AccessToken);
                insertCommand.Parameters.AddWithValue("@refreshToken", userData.RefreshToken);
                insertCommand.Parameters.AddWithValue("@tokenExpirationTime", userData.TokenExpirationTime.ToString("O"));

                await insertCommand.ExecuteNonQueryAsync();
            }
        }

        public static async Task<GameCenter.Commun.UserData> GetUserDataAsync(string steamId)
        {
            using (var db = new SqliteConnection($"Filename={_userDbPath}"))
            {
                await db.OpenAsync();

                var selectCommand = new SqliteCommand(
                    "SELECT * FROM UserData WHERE SteamId = @steamId", db);
                selectCommand.Parameters.AddWithValue("@steamId", steamId);

                using (var reader = await selectCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new UserData
                        {
                            SteamId = reader["SteamId"].ToString(),
                            PersonaName = reader["PersonaName"].ToString(),
                            ProfileUrl = reader["ProfileUrl"].ToString(),
                            AvatarUrl = reader["AvatarUrl"].ToString(),
                            AccessToken = reader["AccessToken"].ToString(),
                            RefreshToken = reader["RefreshToken"].ToString(),
                            TokenExpirationTime = DateTime.Parse(reader["TokenExpirationTime"].ToString())
                        };
                    }
                }
            }

            return null;
        }

        public static async Task SaveGameAsync(Game game)
        {
            using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
            {
                await db.OpenAsync();

                var insertCommand = new SqliteCommand(
                    """
                    INSERT OR REPLACE INTO Games (
                        AppId, Name, Description, ImageUrl, PlaytimeForever, LastPlayed
                    ) VALUES (
                        @appId, @name, @description, @imageUrl, @playtimeForever, @lastPlayed
                    )
                    """, db);

                insertCommand.Parameters.AddWithValue("@appId", game.AppId);
                insertCommand.Parameters.AddWithValue("@name", game.Name);
                insertCommand.Parameters.AddWithValue("@description", game.Description);
                insertCommand.Parameters.AddWithValue("@imageUrl", game.ImageUrl);
                insertCommand.Parameters.AddWithValue("@playtimeForever", game.PlaytimeForever);
                insertCommand.Parameters.AddWithValue("@lastPlayed", game.LastPlayed.ToString("O"));

                await insertCommand.ExecuteNonQueryAsync();
            }
        }

        public static async Task<List<Game>> GetGamesAsync()
        {
            var games = new List<Game>();

            using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
            {
                await db.OpenAsync();

                var selectCommand = new SqliteCommand("SELECT * FROM Games", db);

                using (var reader = await selectCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        games.Add(new Game
                        {
                            AppId = Convert.ToUInt32(reader["AppId"]),
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            ImageUrl = reader["ImageUrl"].ToString(),
                            PlaytimeForever = Convert.ToUInt32(reader["PlaytimeForever"]),
                            LastPlayed = DateTime.Parse(reader["LastPlayed"].ToString())
                        });
                    }
                }
            }

            return games;
        }

        public static async Task<Game> GetGameAsync(uint appId)
        {
            using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
            {
                await db.OpenAsync();

                var selectCommand = new SqliteCommand(
                    "SELECT * FROM Games WHERE AppId = @appId", db);
                selectCommand.Parameters.AddWithValue("@appId", appId);

                using (var reader = await selectCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Game
                        {
                            AppId = Convert.ToUInt32(reader["AppId"]),
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            ImageUrl = reader["ImageUrl"].ToString(),
                            PlaytimeForever = Convert.ToUInt32(reader["PlaytimeForever"]),
                            LastPlayed = DateTime.Parse(reader["LastPlayed"].ToString())
                        };
                    }
                }
            }

            return null;
        }

        public static async Task UpdateGamePlaytimeAsync(uint appId, uint playtime)
        {
            using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
            {
                await db.OpenAsync();

                var updateCommand = new SqliteCommand(
                    """
                    UPDATE Games 
                    SET PlaytimeForever = @playtime, LastPlayed = @lastPlayed
                    WHERE AppId = @appId
                    """, db);

                updateCommand.Parameters.AddWithValue("@playtime", playtime);
                updateCommand.Parameters.AddWithValue("@lastPlayed", DateTime.UtcNow.ToString("O"));
                updateCommand.Parameters.AddWithValue("@appId", appId);

                await updateCommand.ExecuteNonQueryAsync();
            }
        }

        public static async Task DeleteGameAsync(uint appId)
        {
            using (var db = new SqliteConnection($"Filename={_gamesDbPath}"))
            {
                await db.OpenAsync();

                var deleteCommand = new SqliteCommand(
                    "DELETE FROM Games WHERE AppId = @appId", db);

                deleteCommand.Parameters.AddWithValue("@appId", appId);

                await deleteCommand.ExecuteNonQueryAsync();
            }
        }
    }

  
}