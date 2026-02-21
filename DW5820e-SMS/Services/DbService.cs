using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DW5820e_SMS.Models;
using Microsoft.Data.Sqlite;

namespace DW5820e_SMS.Services
{
    /// <summary>
    /// Persists SMS messages to a SQLite database in the application's local data folder.
    /// </summary>
    public sealed class DbService : IDbService
    {
        private readonly string _dbPath;
        private const string TableName = "messages";

        public DbService()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DW5820e-SMS");
            Directory.CreateDirectory(folder);
            _dbPath = Path.Combine(folder, "sms.db");
        }

        // For testing: allow injecting a custom path.
        internal DbService(string dbPath)
        {
            _dbPath = dbPath;
        }

        private SqliteConnection CreateConnection() =>
            new SqliteConnection($"Data Source={_dbPath}");

        public async Task InitializeAsync()
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                CREATE TABLE IF NOT EXISTS {TableName} (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    PhoneNumber TEXT    NOT NULL,
                    Body        TEXT    NOT NULL,
                    Direction   INTEGER NOT NULL,
                    Timestamp   TEXT    NOT NULL,
                    Status      INTEGER NOT NULL,
                    AccountId   TEXT
                );
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> SaveMessageAsync(SmsMessage message)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                INSERT INTO {TableName} (PhoneNumber, Body, Direction, Timestamp, Status, AccountId)
                VALUES ($phone, $body, $dir, $ts, $status, $acct);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("$phone", message.PhoneNumber);
            cmd.Parameters.AddWithValue("$body", message.Body);
            cmd.Parameters.AddWithValue("$dir", (int)message.Direction);
            cmd.Parameters.AddWithValue("$ts", message.Timestamp.ToString("O"));
            cmd.Parameters.AddWithValue("$status", (int)message.Status);
            cmd.Parameters.AddWithValue("$acct", (object?)message.AccountId ?? DBNull.Value);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<SmsMessage>> GetAllMessagesAsync()
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Id, PhoneNumber, Body, Direction, Timestamp, Status, AccountId FROM {TableName} ORDER BY Timestamp ASC;";
            var list = new List<SmsMessage>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new SmsMessage
                {
                    Id = reader.GetInt32(0),
                    PhoneNumber = reader.GetString(1),
                    Body = reader.GetString(2),
                    Direction = (MessageDirection)reader.GetInt32(3),
                    Timestamp = DateTime.Parse(reader.GetString(4)),
                    Status = (MessageStatus)reader.GetInt32(5),
                    AccountId = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
            return list;
        }
    }
}
