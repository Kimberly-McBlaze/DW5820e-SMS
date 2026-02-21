using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DW5820e_SMS.Models;

namespace DW5820e_SMS.Services
{
    /// <summary>
    /// Interface for SMS send/receive operations via the WinRT modem APIs.
    /// </summary>
    public interface ISmsService : IDisposable
    {
        /// <summary>Raised when an inbound SMS arrives.</summary>
        event EventHandler<SmsMessage> MessageReceived;

        /// <summary>Returns all available Mobile Broadband account IDs.</summary>
        IReadOnlyList<string> GetAvailableAccountIds();

        /// <summary>
        /// Opens the SMS device for the given account ID.
        /// Must be called (and awaited) before SendAsync or StartReceiving.
        /// </summary>
        Task OpenAsync(string accountId);

        /// <summary>Sends a text message to <paramref name="toNumber"/>.</summary>
        Task SendAsync(string toNumber, string body);

        /// <summary>Starts listening for inbound messages.</summary>
        void StartReceiving();
    }
}
