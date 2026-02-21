using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DW5820e_SMS.Models;
using Windows.Devices.Sms;
using Windows.Networking.NetworkOperators;

namespace DW5820e_SMS.Services
{
    /// <summary>
    /// Sends and receives SMS through the WinRT SMS APIs
    /// (Windows.Devices.Sms + Windows.Networking.NetworkOperators.MobileBroadbandAccount).
    /// IMPORTANT: WinRT SMS APIs require an STA thread. The Windows App SDK UI thread
    /// is STA, so all public methods must be called from the UI thread (or an STA thread).
    /// </summary>
    public sealed class SmsService : ISmsService
    {
        private SmsDevice2? _device;
        private string? _activeAccountId;

        public event EventHandler<SmsMessage>? MessageReceived;

        public IReadOnlyList<string> GetAvailableAccountIds()
        {
            var ids = MobileBroadbandAccount.AvailableNetworkAccountIds;
            return ids;
        }

        public async Task OpenAsync(string accountId)
        {
            _activeAccountId = accountId;
            _device = await SmsDevice2.FromNetworkAccountIdAsync(accountId);
        }

        public async Task SendAsync(string toNumber, string body)
        {
            if (_device is null)
                throw new InvalidOperationException("SMS device not open. Call OpenAsync first.");

            var msg = new SmsTextMessage2();
            msg.To = toNumber;
            msg.Body = body;
            await _device.SendMessageAndGetResultAsync(msg);
        }

        public void StartReceiving()
        {
            if (_device is null)
                throw new InvalidOperationException("SMS device not open. Call OpenAsync first.");

            _device.MessageReceived += OnDeviceMessageReceived;
        }

        private void OnDeviceMessageReceived(SmsDevice2 sender, SmsMessageReceivedTriggerDetails args)
        {
            if (args.MessageType != SmsMessageType.Text)
                return;

            var raw = args.TextMessage;
            if (raw is null)
                return;

            var msg = new SmsMessage
            {
                PhoneNumber = raw.From,
                Body = raw.Body,
                Direction = MessageDirection.Inbound,
                Timestamp = raw.Timestamp.LocalDateTime,
                Status = MessageStatus.Received,
                AccountId = _activeAccountId
            };
            MessageReceived?.Invoke(this, msg);
        }

        public void Dispose()
        {
            if (_device is not null)
            {
                _device.MessageReceived -= OnDeviceMessageReceived;
                _device.Dispose();
                _device = null;
            }
        }
    }
}
