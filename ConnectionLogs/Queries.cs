﻿using ConnectedPlayers;
using MySqlConnector;

namespace ConnectionLogs
{
    internal class Queries
    {
        private static bool DatabaseConnected = DatabaseConnection();

        /// <summary>
        /// Checks if a connection to the database can be established and creates a table if it doesn't exist.
        /// </summary>
        /// <returns>True if a connection to the database was established and the table was created, false otherwise.</returns>
        private static bool DatabaseConnection()
        {
            try
            {
                using var connection = Database.GetConnection();
                connection.Open();
                CreateTable(connection);
                connection.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Inserts a new user into the database or updates an existing user's client name.
        /// </summary>
        /// <param name="steamId">The Steam ID of the user.</param>
        /// <param name="clientName">The name of the client.</param>
        public static void InsertUser(string steamId, string clientName)
        {
            if (!DatabaseConnected)
            {
                return;
            }

            if (UserExists(steamId))
            {
                UpdateUser(steamId, clientName);
                return;
            }

            using var connection = Database.GetConnection();

            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (SteamId, ClientName) VALUES (@steamId, @clientName);";
            command.Parameters.AddWithValue("@steamId", steamId);
            command.Parameters.AddWithValue("@clientName", clientName);

            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }

        /// <summary>
        /// Checks if a user with the given Steam ID exists in the database.
        /// </summary>
        /// <param name="steamId">The Steam ID of the user to check.</param>
        /// <returns>True if the user exists, false otherwise.</returns>
        public static bool UserExists(string steamId)
        {
            using var connection = Database.GetConnection();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Users WHERE SteamId = @steamId;";
            command.Parameters.AddWithValue("@steamId", steamId);

            connection.Open();
            var result = command.ExecuteScalar();
            connection.Close();

            return Convert.ToInt32(result) > 0;
        }

 
        /// <summary>
        /// Updates the user with the given Steam ID and client name in the database.
        /// </summary>
        /// <param name="steamId">The Steam ID of the user to update.</param>
        /// <param name="clientName">The name of the client to update for the user.</param>
        private static void UpdateUser(string steamId, string clientName)
        {
            // Repetitive call that isn't actually needed since it's already checked in InsertUser
            /*
                if (!UserExists(steamId))
                {
                    return;
                }
            */

            using var connection = Database.GetConnection();

            using var command = connection.CreateCommand();
            // It should update the ConnectedAt automatically, but yeah it doesn't work
            command.CommandText = "UPDATE Users SET ClientName = @clientName, ConnectedAt = CURRENT_TIMESTAMP WHERE SteamId = @steamId;";
            command.Parameters.AddWithValue("@steamId", steamId);
            // Escpae the shit out of this
            command.Parameters.AddWithValue("@clientName", MySqlHelper.EscapeString(clientName));

            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }

        // This shouldn't be static, but i'm making the call inside of a static method, so yeah 
        /// <summary>
        /// Creates a table named "Users" in the provided MySQL connection if it doesn't exist already.
        /// The table has columns for Id (auto-incrementing integer), SteamId (string), ClientName (string), and ConnectedAt (timestamp).
        /// SteamId is set as a unique key.
        /// </summary>
        /// <param name="connection">The MySqlConnection object to use for creating the table.</param>
        private static void CreateTable(MySqlConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"CREATE TABLE IF NOT EXISTS `Users` (
                                        `Id` int(11) NOT NULL AUTO_INCREMENT,
                                        `SteamId` varchar(18) NOT NULL,
                                        `ClientName` varchar(128) NOT NULL,
                                        `ConnectedAt` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
                                        PRIMARY KEY (`Id`),
                                        UNIQUE KEY `SteamId` (`SteamId`)
                                    );";

            command.ExecuteNonQuery();
            command.Dispose();
        }

        /// <summary>
        /// Retrieves a list of the 50 most recently connected users from the database.
        /// </summary>
        /// <returns>A list of User objects representing the connected players.</returns>
        public static List<User> GetConnectedPlayers()
        {
            if (!DatabaseConnected)
            {
                return new();
            }

            using var connection = Database.GetConnection();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Users ORDER BY ConnectedAt DESC LIMIT 50;";

            connection.Open();
            var reader = command.ExecuteReader();
            var users = new List<User>();
            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32("Id"),
                    SteamId = reader.GetString("SteamId"),
                    ClientName = reader.GetString("ClientName"),
                    ConnectedAt = reader.GetDateTime("ConnectedAt")
                });
            }

            connection.Close();

            return users;
        }
    }
}
