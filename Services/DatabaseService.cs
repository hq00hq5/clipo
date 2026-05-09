using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Clipo.Models;

namespace Clipo.Services
{
    public class DatabaseService
    {
        private const string ConnectionString = "Data Source=clipo.db";

        public DatabaseService()
        {
            InitializeDatabase();
        }

        private SqliteConnection CreateConnection()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA synchronous=OFF;";
            cmd.ExecuteNonQuery();
            return conn;
        }

        private void InitializeDatabase()
        {
            using var connection = CreateConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                PRAGMA journal_mode=WAL;
                CREATE TABLE IF NOT EXISTS ClipboardHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Text TEXT NOT NULL UNIQUE,
                    Timestamp DATETIME NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IDX_Timestamp ON ClipboardHistory(Timestamp DESC);
                
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );
            ";
            command.ExecuteNonQuery();
        }

        public void AddOrUpdateItem(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            using var connection = CreateConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO ClipboardHistory (Text, Timestamp)
                VALUES (@text, @timestamp);
            ";
            command.Parameters.AddWithValue("@text", text);
            command.Parameters.AddWithValue("@timestamp", DateTime.Now);
            command.ExecuteNonQuery();

            // Enforce max 10,000 items as requested
            var trimCmd = connection.CreateCommand();
            trimCmd.CommandText = @"
                DELETE FROM ClipboardHistory 
                WHERE Id NOT IN (
                    SELECT Id FROM ClipboardHistory ORDER BY Timestamp DESC LIMIT 10000
                );
            ";
            trimCmd.ExecuteNonQuery();
        }

        public List<ClipboardItem> SearchItems(string query = "", int limit = 100)
        {
            var results = new List<ClipboardItem>();
            using var connection = CreateConnection();

            var command = connection.CreateCommand();
            
            if (string.IsNullOrWhiteSpace(query))
            {
                command.CommandText = "SELECT Id, Text, Timestamp FROM ClipboardHistory ORDER BY Timestamp DESC LIMIT @limit";
            }
            else
            {
                command.CommandText = @"
                    SELECT Id, Text, Timestamp FROM ClipboardHistory 
                    WHERE Text LIKE @query 
                    ORDER BY Timestamp DESC LIMIT @limit";
                command.Parameters.AddWithValue("@query", $"%{query}%");
            }
            
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new ClipboardItem
                {
                    Id = reader.GetInt32(0),
                    Text = reader.GetString(1),
                    Timestamp = reader.GetDateTime(2)
                });
            }

            return results;
        }

        public void DeleteItem(int id)
        {
            using var connection = CreateConnection();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ClipboardHistory WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            using var connection = CreateConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Settings WHERE Key = @key";
            command.Parameters.AddWithValue("@key", key);
            var result = command.ExecuteScalar();
            return result?.ToString() ?? defaultValue;
        }

        public void SetSetting(string key, string value)
        {
            using var connection = CreateConnection();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@key, @value)";
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", value);
            command.ExecuteNonQuery();
        }
    }
}
