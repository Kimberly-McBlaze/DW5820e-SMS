using System;
using System.Linq;
using System.Threading.Tasks;
using DW5820e_SMS.Services;
using DW5820e_SMS.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace DW5820e_SMS
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            var db = new DbService();
            var sms = new SmsService();
            _vm = new MainViewModel(db, sms, DispatcherQueue.GetForCurrentThread());

            MessageList.ItemsSource = _vm.Messages;
            AccountCombo.ItemsSource = _vm.AccountIds;

            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            AccountProgress.IsActive = true;
            StatusText.Text = string.Empty;
            try
            {
                await _vm.InitializeAsync();
                if (_vm.AccountIds.Count == 0)
                {
                    StatusText.Text = "No Mobile Broadband account found. Ensure the DW5820e modem is connected and mobile data is enabled.";
                }
                else if (_vm.AccountIds.Count == 1)
                {
                    AccountCombo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Initialization error: {ex.Message}";
            }
            finally
            {
                AccountProgress.IsActive = false;
            }
        }

        private async void AccountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountCombo.SelectedItem is not string accountId)
                return;

            AccountProgress.IsActive = true;
            StatusText.Text = string.Empty;
            try
            {
                await _vm.SelectAccountAsync(accountId);
                StatusText.Text = string.Empty;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Could not open SMS device: {ex.Message}";
            }
            finally
            {
                AccountProgress.IsActive = false;
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var to = ToBox.Text?.Trim() ?? string.Empty;
            var body = BodyBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(body))
            {
                StatusText.Text = "Please enter both a phone number and a message.";
                return;
            }

            SendButton.IsEnabled = false;
            SendProgress.IsActive = true;
            StatusText.Text = string.Empty;
            try
            {
                await _vm.SendMessageAsync(to, body);
                BodyBox.Text = string.Empty;
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Send failed: {ex.Message}";
            }
            finally
            {
                SendButton.IsEnabled = true;
                SendProgress.IsActive = false;
            }
        }

        private async void ExportCsvButton_Click(object sender, RoutedEventArgs e)
            => await ExportAsync("CSV", "csv", "text/csv",
                path => ExportService.ExportCsv(_vm.GetAllMessages(), path));

        private async void ExportJsonButton_Click(object sender, RoutedEventArgs e)
            => await ExportAsync("JSON", "json", "application/json",
                path => ExportService.ExportJson(_vm.GetAllMessages(), path));

        private async Task ExportAsync(string label, string ext, string mime, Action<string> write)
        {
            var picker = new FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = $"sms-export-{DateTime.Now:yyyyMMdd-HHmmss}";
            picker.FileTypeChoices.Add(label, new System.Collections.Generic.List<string> { $".{ext}" });

            var file = await picker.PickSaveFileAsync();
            if (file is null)
                return;

            StatusText.Text = string.Empty;
            try
            {
                write(file.Path);
                StatusText.Text = $"Exported to {file.Path}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Export failed: {ex.Message}";
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = string.Empty;
            try
            {
                await _vm.RefreshAsync();
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Refresh failed: {ex.Message}";
            }
        }

        private void ScrollToBottom()
        {
            if (_vm.Messages.Count > 0)
                MessageList.ScrollIntoView(_vm.Messages.Last());
        }
    }
}
