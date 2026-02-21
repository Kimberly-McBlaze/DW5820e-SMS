using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DW5820e_SMS.Models;
using DW5820e_SMS.Services;
using Microsoft.UI.Dispatching;

namespace DW5820e_SMS.ViewModels
{
    public sealed class MainViewModel
    {
        private readonly IDbService _db;
        private readonly ISmsService _sms;
        private readonly DispatcherQueue _dispatcher;
        private string? _activeAccountId;

        public ObservableCollection<SmsMessage> Messages { get; } = new();
        public ObservableCollection<string> AccountIds { get; } = new();

        public MainViewModel(IDbService db, ISmsService sms, DispatcherQueue dispatcher)
        {
            _db = db;
            _sms = sms;
            _dispatcher = dispatcher;
            _sms.MessageReceived += OnMessageReceived;
        }

        public async Task InitializeAsync()
        {
            await _db.InitializeAsync();
            var history = await _db.GetAllMessagesAsync();
            foreach (var m in history)
                Messages.Add(m);

            var ids = _sms.GetAvailableAccountIds();
            foreach (var id in ids)
                AccountIds.Add(id);
        }

        public async Task SelectAccountAsync(string accountId)
        {
            _activeAccountId = accountId;
            await _sms.OpenAsync(accountId);
            _sms.StartReceiving();
        }

        public async Task SendMessageAsync(string toNumber, string body)
        {
            if (string.IsNullOrWhiteSpace(toNumber))
                throw new ArgumentException("Phone number is required.", nameof(toNumber));
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Message body is required.", nameof(body));

            var msg = new SmsMessage
            {
                PhoneNumber = toNumber.Trim(),
                Body = body.Trim(),
                Direction = MessageDirection.Outbound,
                Timestamp = DateTime.UtcNow,
                Status = MessageStatus.Pending,
                AccountId = _activeAccountId
            };

            msg.Id = await _db.SaveMessageAsync(msg);
            Messages.Add(msg);

            try
            {
                await _sms.SendAsync(msg.PhoneNumber, msg.Body);
                msg.Status = MessageStatus.Sent;
            }
            catch
            {
                msg.Status = MessageStatus.Failed;
                throw;
            }
        }

        public IReadOnlyList<SmsMessage> GetAllMessages() => Messages;

        public async Task RefreshAsync()
        {
            Messages.Clear();
            var msgs = await _db.GetAllMessagesAsync();
            foreach (var m in msgs)
                Messages.Add(m);
        }

        private void OnMessageReceived(object? sender, SmsMessage msg)
        {
            // Save to DB on a background task, then marshal to UI thread.
            _ = Task.Run(async () =>
            {
                msg.Id = await _db.SaveMessageAsync(msg);
                _dispatcher.TryEnqueue(() => Messages.Add(msg));
            });
        }

        public void Dispose()
        {
            _sms.MessageReceived -= OnMessageReceived;
            _sms.Dispose();
        }
    }
}
