using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace GameCenter.Helpers
{
    public static class TestDataGenerator
    {
        public static void PopulateTestData()
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var gameCenterPath = Path.Combine(documentsPath, "GameCenter");
            var gamesDbPath = Path.Combine(gameCenterPath, "GamesMetaData.db");
            var gamesImagesPath = Path.Combine(gameCenterPath, "gamesimages");

            // Ensure the directory exists
            Directory.CreateDirectory(gameCenterPath);
            Directory.CreateDirectory(gamesImagesPath);

            using (var connection = new SqliteConnection($"Data Source={gamesDbPath}"))
            {
                connection.Open();

                // Create tables if they don't exist
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Games (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Subtitle TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        Launcher TEXT NOT NULL,
                        LaunchUri TEXT NOT NULL
                    )");

                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS DLC (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        GameId INTEGER NOT NULL,
                        Title TEXT NOT NULL,
                        Price TEXT NOT NULL,
                        FOREIGN KEY (GameId) REFERENCES Games(Id)
                    )");

                // Clear existing data
                ExecuteNonQuery(connection, "DELETE FROM DLC");
                ExecuteNonQuery(connection, "DELETE FROM Games");

                // Insert sample games
                InsertGame(connection, "Cyberpunk 2077", "Action RPG", "Experience the future in this open-world adventure.", "Steam", "steam://rungameid/1091500");
                InsertGame(connection, "The Witcher 3", "Action RPG", "Embark on an epic journey in a fantasy world.", "GOG", "gog://1207664663");
                InsertGame(connection, "Red Dead Redemption 2", "Action-Adventure", "Live the life of an outlaw in the Wild West.", "Rockstar", "rockstar://launchplatform/rdr2");
                InsertGame(connection, "Hades", "Roguelike", "Battle out of Hell in this award-winning roguelike.", "Epic", "com.epicgames.launcher://apps/min?action=launch&appId=Hades");

                // Insert sample DLCs
                InsertDLC(connection, 1, "Night City Expansion", "$19.99");
                InsertDLC(connection, 2, "Blood and Wine", "$19.99");
                InsertDLC(connection, 2, "Hearts of Stone", "$9.99");
                InsertDLC(connection, 3, "Online Mode", "Free");
            }

            // Create placeholder images
            CreatePlaceholderImages(gamesImagesPath, "Cyberpunk 2077");
            CreatePlaceholderImages(gamesImagesPath, "The Witcher 3");
            CreatePlaceholderImages(gamesImagesPath, "Red Dead Redemption 2");
            CreatePlaceholderImages(gamesImagesPath, "Hades");

            Console.WriteLine("Test data has been populated successfully.");
        }

        private static void ExecuteNonQuery(SqliteConnection connection, string commandText)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
        }

        private static void InsertGame(SqliteConnection connection, string title, string subtitle, string description, string launcher, string launchUri)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO Games (Title, Subtitle, Description, Launcher, LaunchUri)
                    VALUES (@Title, @Subtitle, @Description, @Launcher, @LaunchUri)";
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Subtitle", subtitle);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@Launcher", launcher);
                command.Parameters.AddWithValue("@LaunchUri", launchUri);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertDLC(SqliteConnection connection, int gameId, string title, string price)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO DLC (GameId, Title, Price)
                    VALUES (@GameId, @Title, @Price)";
                command.Parameters.AddWithValue("@GameId", gameId);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Price", price);
                command.ExecuteNonQuery();
            }
        }

        private static void CreatePlaceholderImages(string gamesImagesPath, string gameTitle)
        {
            var gamePath = Path.Combine(gamesImagesPath, gameTitle);
            Directory.CreateDirectory(gamePath);

        }

      
    }
}