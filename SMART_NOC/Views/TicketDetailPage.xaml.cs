using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using SMART_NOC.Models;
using SMART_NOC.Services;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Microsoft.UI;
using Windows.UI;

namespace SMART_NOC.Views
{
    public sealed partial class TicketDetailPage : Page
    {
        public TicketLog Ticket { get; set; } = new TicketLog();
        private HistoryService _historyService = new HistoryService();

        // Helper Properties
        public SolidColorBrush StatusBackground
        {
            get
            {
                string s = Ticket?.Status?.ToUpper() ?? "";
                if (s.Contains("DOWN")) return new SolidColorBrush(Color.FromArgb(255, 255, 69, 58));
                if (s.Contains("UP") || s.Contains("CLOSED")) return new SolidColorBrush(Color.FromArgb(255, 48, 209, 88));
                return new SolidColorBrush(Color.FromArgb(255, 255, 159, 10));
            }
        }

        public string StatusIcon
        {
            get
            {
                string s = Ticket?.Status?.ToUpper() ?? "";
                if (s.Contains("DOWN")) return "\uE711";
                if (s.Contains("UP") || s.Contains("CLOSED")) return "\uE73E";
                return "\uE7BA";
            }
        }

        public SolidColorBrush ClosedDotColor
        {
            get
            {
                return string.IsNullOrEmpty(Ticket.ClosedTime)
                    ? new SolidColorBrush(Color.FromArgb(100, 128, 128, 128))
                    : new SolidColorBrush(Color.FromArgb(255, 48, 209, 88));
            }
        }

        public TicketDetailPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is TicketLog ticket)
            {
                // 1. Tampilkan data "sementara" (yang dioper dari halaman sebelumnya)
                this.Ticket = ticket;
                if (this.Ticket.ImpactDetail == null) this.Ticket.ImpactDetail = new List<ImpactItem>();
                this.Bindings.Update();
                LoadTicketImage();

                // 2. 🔥 REFRESH DATA DARI DATABASE (PENTING) 🔥
                // Ini akan mengambil update terbaru (termasuk kronologis baru) dari file JSON
                await RefreshTicketData(ticket.TT_IOH);
            }
        }

        private async Task RefreshTicketData(string ticketId)
        {
            try
            {
                // Ambil semua tiket terbaru dari service
                var allTickets = await _historyService.GetAllTicketsAsync();

                // Cari tiket yang ID-nya sama dengan yang sedang dibuka
                var freshTicket = allTickets.FirstOrDefault(t => t.TT_IOH == ticketId);

                if (freshTicket != null)
                {
                    // Timpa data lama dengan data baru
                    this.Ticket = freshTicket;
                    if (this.Ticket.ImpactDetail == null) this.Ticket.ImpactDetail = new List<ImpactItem>();

                    // Update Tampilan (Refresh UI Binding)
                    this.Bindings.Update();
                    LoadTicketImage(); // Reload gambar barangkali ada update gambar
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gagal refresh data tiket: {ex.Message}");
            }
        }

        // 🔥 REFRESH BUTTON (NEW) 🔥
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            BtnRefresh.IsEnabled = false;
            try
            {
                await RefreshTicketData(Ticket.TT_IOH);
                
                // Show success notification
                var dialog = new ContentDialog
                {
                    Title = "✅ Data Refreshed",
                    Content = "Ticket data has been updated from database.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to refresh: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            finally
            {
                BtnRefresh.IsEnabled = true;
            }
        }

        // 🔥 SHARE BUTTON (NEW) 🔥
        private async void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            if (Ticket == null) return;

            string shareContent = $"""
[SMART NOC - INCIDENT REPORT]

📋 Ticket ID: {Ticket.TT_IOH}
📍 Segment: {Ticket.SegmentPM}
🔴 Status: {Ticket.Status}
⏱️ MTTR: {GetMttrDuration()}

🏢 Region: {Ticket.Region}
📍 Cut Point: {Ticket.CutPoint}
🎯 Root Cause: {Ticket.RootCause}

👤 PIC: {Ticket.PicInfo}
🕐 Timeline:
  • Occur: {Ticket.OccurTime}
  • Dispatch: {Ticket.DispatchTime}
  • Closed: {Ticket.ClosedTime}
""";

            var dataPackage = new DataPackage();
            dataPackage.SetText(shareContent);
            Clipboard.SetContent(dataPackage);

            var dialog = new ContentDialog
            {
                Title = "✅ Copied to Clipboard",
                Content = "Detailed ticket information has been copied.",
                CloseButtonText = "Close",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        public string GetMttrDuration()
        {
            if (DateTime.TryParse(Ticket.OccurTime, out DateTime start))
            {
                DateTime end = DateTime.Now;
                if (!string.IsNullOrEmpty(Ticket.ClosedTime) && DateTime.TryParse(Ticket.ClosedTime, out DateTime c))
                {
                    end = c;
                }

                TimeSpan d = end - start;
                string suffix = string.IsNullOrEmpty(Ticket.ClosedTime) ? " (Running)" : "";

                if (d.TotalHours < 1) return $"{d.Minutes}m{suffix}";
                return $"{(int)d.TotalHours}h {d.Minutes}m{suffix}";
            }
            return "-";
        }

        public Visibility GetEmptyStateVisibility(int count) => count == 0 ? Visibility.Visible : Visibility.Collapsed;

        private void LoadTicketImage()
        {
            if (EvidenceSection != null) EvidenceSection.Visibility = Visibility.Collapsed;
            try
            {
                if (!string.IsNullOrEmpty(Ticket.ImagePath) && File.Exists(Ticket.ImagePath))
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(Ticket.ImagePath));
                    if (ImgEvidence != null) ImgEvidence.Source = bitmap;
                    if (EvidenceSection != null) EvidenceSection.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }

        // ACTION HANDLERS
        private void BtnBack_Click(object sender, RoutedEventArgs e) 
        { 
            if (Frame.CanGoBack) Frame.GoBack(); 
        }

        private void BtnLoadToForm_Click(object sender, RoutedEventArgs e) 
        { 
            if (Ticket != null) Frame.Navigate(typeof(TicketPage), Ticket); 
        }

        private async void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (Ticket != null)
            {
                var dp = new DataPackage();
                string content = $"""
[SMART NOC] INCIDENT REPORT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 Ticket ID:      {Ticket.TT_IOH}
📍 Segment PM:     {Ticket.SegmentPM}
🔴 Status:         {Ticket.Status}
⏱️  MTTR Duration:  {GetMttrDuration()}

🏢 Region:         {Ticket.Region}
🎯 Root Cause:     {Ticket.RootCause}
📍 Cut Point:      {Ticket.CutPoint}

👤 Field Team:     {Ticket.PicInfo}
📍 Mandau Ref:     {Ticket.TT_Mandau}

🕐 Timeline:
   Occur Time:     {Ticket.OccurTime}
   Dispatch Time:  {Ticket.DispatchTime}
   Closed Time:    {Ticket.ClosedTime}

📝 Updates:
{Ticket.Updates}
""";

                dp.SetText(content);
                Clipboard.SetContent(dp);

                var dialog = new ContentDialog
                {
                    Title = "✅ Copied",
                    Content = "Ticket summary copied to clipboard.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private async void BtnPdf_Click(object sender, RoutedEventArgs e)
        {
            if (Ticket == null) return;
            try
            {
                BtnPdf.IsEnabled = false;
                var pdfService = new PdfService();
                string filePath = await pdfService.GenerateBapsAsync(Ticket);

                ContentDialog dialog = new ContentDialog
                {
                    Title = "✅ BAPS Generated Successfully",
                    Content = $"File saved at:\n{filePath}",
                    PrimaryButtonText = "Open File",
                    CloseButtonText = "Close",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(filePath));
                }
            }
            catch (Exception ex)
            {
                ContentDialog d = new ContentDialog 
                { 
                    Title = "❌ Error", 
                    Content = $"Failed to generate PDF:\n{ex.Message}", 
                    CloseButtonText = "OK", 
                    XamlRoot = this.Content.XamlRoot 
                };
                await d.ShowAsync();
            }
            finally 
            { 
                BtnPdf.IsEnabled = true; 
            }
        }
    }
}