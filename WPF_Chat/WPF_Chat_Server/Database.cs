using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace WPF_Chat_Server
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string dbPath)
        {
            _connectionString = $"Data Source={dbPath}";
        }

        // --- Users ---

        public bool RegisterUser(string username, string password)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO Users (Username, Password) VALUES ($u, $p)";
                cmd.Parameters.AddWithValue("$u", username);
                cmd.Parameters.AddWithValue("$p", password);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false; // Username already exists
            }
        }

        public int? LoginUser(string username, string password)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id FROM Users WHERE Username = $u AND Password = $p";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$p", password);
            var result = cmd.ExecuteScalar();
            return result is long id ? (int)id : null;
        }

        public void UpdateUserProfile(int userId, string color, string imageBase64)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Users SET Color = $c, Image = $i WHERE Id = $id";
            cmd.Parameters.AddWithValue("$c", color);
            cmd.Parameters.AddWithValue("$i", imageBase64);
            cmd.Parameters.AddWithValue("$id", userId);
            cmd.ExecuteNonQuery();
        }

        public (string Color, string Image) GetUserProfile(int userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Color, Image FROM Users WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", userId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return (reader.GetString(0), reader.GetString(1));
            return ("#000000", "");
        }

        // --- Rooms ---

        public bool CreateRoom(string name)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO Rooms (Name) VALUES ($n)";
                cmd.Parameters.AddWithValue("$n", name);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public List<(int Id, string Name)> GetRooms()
        {
            var rooms = new List<(int, string)>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Rooms";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                rooms.Add((reader.GetInt32(0), reader.GetString(1)));
            return rooms;
        }

        // --- Messages ---

        public void SaveMessage(int roomId, int senderId, string text)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Messages (RoomId, SenderId, Text, Timestamp) VALUES ($r, $s, $t, $ts)";
            cmd.Parameters.AddWithValue("$r", roomId);
            cmd.Parameters.AddWithValue("$s", senderId);
            cmd.Parameters.AddWithValue("$t", text);
            cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public void SavePrivateMessage(int senderId, int receiverId, string text)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO PrivateMessages (SenderId, ReceiverId, Text, Timestamp) VALUES ($s, $r, $t, $ts)";
            cmd.Parameters.AddWithValue("$s", senderId);
            cmd.Parameters.AddWithValue("$r", receiverId);
            cmd.Parameters.AddWithValue("$t", text);
            cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }
    }
}
