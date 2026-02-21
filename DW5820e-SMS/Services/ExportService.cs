using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DW5820e_SMS.Models;

namespace DW5820e_SMS.Services
{
    /// <summary>
    /// Exports a list of SMS messages to CSV or JSON.
    /// </summary>
    public static class ExportService
    {
        public static void ExportCsv(IEnumerable<SmsMessage> messages, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,PhoneNumber,Direction,Timestamp,Status,Body");
            foreach (var m in messages)
            {
                sb.AppendLine(string.Join(",",
                    m.Id,
                    CsvEscape(m.PhoneNumber),
                    m.Direction,
                    m.Timestamp.ToString("O", CultureInfo.InvariantCulture),
                    m.Status,
                    CsvEscape(m.Body)));
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static void ExportJson(IEnumerable<SmsMessage> messages, string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(messages, options);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        private static string CsvEscape(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
