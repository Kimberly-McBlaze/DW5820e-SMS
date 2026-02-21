using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DW5820e_SMS.Models;
using DW5820e_SMS.Services;
using Xunit;

namespace DW5820e_SMS.Tests
{
    public sealed class DbServiceTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly DbService _db;

        public DbServiceTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"sms-test-{Guid.NewGuid():N}.db");
            _db = new DbService(_dbPath);
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }

        [Fact]
        public async Task InitializeAsync_CreatesTable()
        {
            await _db.InitializeAsync();
            var messages = await _db.GetAllMessagesAsync();
            Assert.NotNull(messages);
            Assert.Empty(messages);
        }

        [Fact]
        public async Task SaveMessageAsync_ReturnsPositiveId()
        {
            await _db.InitializeAsync();
            var msg = MakeMessage("555-1234", "Hello world");
            var id = await _db.SaveMessageAsync(msg);
            Assert.True(id > 0);
        }

        [Fact]
        public async Task SaveAndRetrieve_RoundTrip()
        {
            await _db.InitializeAsync();
            var ts = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Local);
            var msg = new SmsMessage
            {
                PhoneNumber = "+12025551234",
                Body = "Test message",
                Direction = MessageDirection.Outbound,
                Timestamp = ts,
                Status = MessageStatus.Sent,
                AccountId = "acct-001"
            };
            msg.Id = await _db.SaveMessageAsync(msg);
            var all = await _db.GetAllMessagesAsync();
            Assert.Single(all);
            var r = all[0];
            Assert.Equal(msg.Id, r.Id);
            Assert.Equal(msg.PhoneNumber, r.PhoneNumber);
            Assert.Equal(msg.Body, r.Body);
            Assert.Equal(msg.Direction, r.Direction);
            Assert.Equal(msg.Status, r.Status);
            Assert.Equal(msg.AccountId, r.AccountId);
            Assert.Equal(ts.ToUniversalTime(), r.Timestamp.ToUniversalTime());
        }

        [Fact]
        public async Task MultipleMessages_OrderedByTimestamp()
        {
            await _db.InitializeAsync();
            var t1 = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            var t2 = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
            await _db.SaveMessageAsync(MakeMessage("A", "First", t2));
            await _db.SaveMessageAsync(MakeMessage("B", "Second", t1));
            var all = await _db.GetAllMessagesAsync();
            Assert.Equal(2, all.Count);
            Assert.True(all[0].Timestamp.ToUniversalTime() <= all[1].Timestamp.ToUniversalTime());
        }

        private static SmsMessage MakeMessage(string phone, string body,
            DateTime? ts = null) => new()
        {
            PhoneNumber = phone,
            Body = body,
            Direction = MessageDirection.Inbound,
            Timestamp = ts ?? DateTime.UtcNow,
            Status = MessageStatus.Received
        };
    }
}
