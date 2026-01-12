using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SMART_NOC.Services;
using System;
using System.Threading.Tasks; // Tambahin ini buat Task

namespace SMART_NOC.Views
{
    public sealed partial class HandoverPage : Page
    {
        private HandoverService _service = new HandoverService();

        public HandoverPage()
        {
            this.InitializeComponent();
            LoadLogs();
        }

        private async void LoadLogs()
        {
            var logs = await _service.GetLogsAsync();
            LvLogs.ItemsSource = logs;
        }

        private async void BtnSendLog_Click(object sender, RoutedEventArgs e)
        {
            await SendLog();
        }

        private async void TxtLogMessage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await SendLog();
            }
        }

        private async Task SendLog()
        {
            if (string.IsNullOrWhiteSpace(TxtLogMessage.Text)) return;

            // Matikan tombol biar gak double submit
            BtnSendLog.IsEnabled = false;

            // Ambil value Checkbox (Safe null check)
            bool isUrgent = ChkUrgent.IsChecked ?? false;

            await _service.AddLogAsync(TxtLogMessage.Text, isUrgent);

            // Reset Input
            TxtLogMessage.Text = "";
            ChkUrgent.IsChecked = false;

            // Refresh List
            LoadLogs();

            // Hidupkan tombol lagi
            BtnSendLog.IsEnabled = true;
        }
    }
}