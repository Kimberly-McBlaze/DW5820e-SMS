using System;

namespace DW5820e_SMS.Models
{
    public enum MessageDirection { Inbound, Outbound }
    public enum MessageStatus { Pending, Sent, Failed, Received }

    public class SmsMessage
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public MessageDirection Direction { get; set; }
        public DateTime Timestamp { get; set; }
        public MessageStatus Status { get; set; }
        public string? AccountId { get; set; }

        public string DirectionLabel => Direction == MessageDirection.Inbound ? "↓ In" : "↑ Out";
        public string StatusLabel => Status.ToString();
    }
}
