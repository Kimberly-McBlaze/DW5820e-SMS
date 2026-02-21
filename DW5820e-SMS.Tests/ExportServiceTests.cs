using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DW5820e_SMS.Models;
using DW5820e_SMS.Services;
using Xunit;

namespace DW5820e_SMS.Tests
{
    public sealed class ExportServiceTests : IDisposable
    {
        private readonly string _tempDir;

        public ExportServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"sms-export-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private static List<SmsMessage> SampleMessages() => new()
        {
            new SmsMessage
            {
                Id = 1,
                PhoneNumber = "+12025551111",
                Body = "Hello",
                Direction = MessageDirection.Outbound,
                Timestamp = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                Status = MessageStatus.Sent,
                AccountId = "acct-1"
            },
            new SmsMessage
            {
                Id = 2,
                PhoneNumber = "+12025552222",
                Body = "Reply, with \"quotes\" and,comma",
                Direction = MessageDirection.Inbound,
                Timestamp = new DateTime(2025, 6, 1, 10, 5, 0, DateTimeKind.Utc),
                Status = MessageStatus.Received
            }
        };

        [Fact]
        public void ExportCsv_CreatesFile()
        {
            var path = Path.Combine(_tempDir, "test.csv");
            ExportService.ExportCsv(SampleMessages(), path);
            Assert.True(File.Exists(path));
        }

        [Fact]
        public void ExportCsv_ContainsHeader()
        {
            var path = Path.Combine(_tempDir, "test.csv");
            ExportService.ExportCsv(SampleMessages(), path);
            var lines = File.ReadAllLines(path);
            Assert.StartsWith("Id,PhoneNumber,Direction,Timestamp,Status,Body", lines[0]);
        }

        [Fact]
        public void ExportCsv_CorrectRowCount()
        {
            var path = Path.Combine(_tempDir, "test.csv");
            var msgs = SampleMessages();
            ExportService.ExportCsv(msgs, path);
            var lines = File.ReadAllLines(path);
            // 1 header + N data rows
            Assert.Equal(msgs.Count + 1, lines.Length);
        }

        [Fact]
        public void ExportCsv_EscapesSpecialChars()
        {
            var path = Path.Combine(_tempDir, "escape.csv");
            ExportService.ExportCsv(SampleMessages(), path);
            var content = File.ReadAllText(path);
            // The body with quotes and comma should be wrapped in quotes
            Assert.Contains("\"Reply, with \"\"quotes\"\" and,comma\"", content);
        }

        [Fact]
        public void ExportJson_CreatesValidJson()
        {
            var path = Path.Combine(_tempDir, "test.json");
            ExportService.ExportJson(SampleMessages(), path);
            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path);
            var parsed = JsonSerializer.Deserialize<List<JsonElement>>(content);
            Assert.NotNull(parsed);
            Assert.Equal(2, parsed!.Count);
        }

        [Fact]
        public void ExportJson_PreservesPhoneNumber()
        {
            var path = Path.Combine(_tempDir, "test.json");
            ExportService.ExportJson(SampleMessages(), path);
            var content = File.ReadAllText(path);
            Assert.Contains("+12025551111", content);
        }
    }
}
