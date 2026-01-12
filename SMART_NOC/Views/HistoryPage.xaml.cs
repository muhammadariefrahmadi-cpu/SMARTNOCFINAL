using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SMART_NOC.Models;
using SMART_NOC.Services;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Controls;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System.Threading; // Wajib untuk CancellationToken

namespace SMART_NOC.Views
{
    public sealed partial class HistoryPage : Page
    {
        private HistoryService _service = new HistoryService();
        private List<TicketLog> _allTickets = new List<TicketLog>();

        // Token untuk membatalkan proses (Delete / Import)
        private CancellationTokenSource? _cts;

        public ObservableCollection<TicketLog> FilteredTickets { get; set; } = new ObservableCollection<TicketLog>();

        public HistoryPage()
        {
            // 🔥 TRAP 1: InitializeComponent
            try
            {
                this.InitializeComponent();
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "HISTORY_PAGE_XAML_INIT");
                throw;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "HISTORY_PAGE_NAVIGATED");
            }
        }

        private async void LoadData()
        {
            try
            {
                if (LoadingRing != null) LoadingRing.IsActive = true;
                if (EmptyState != null) EmptyState.Visibility = Visibility.Collapsed;

                if (MainWindow.Instance != null) MainWindow.Instance.AddLog("📂 Loading History Data...");

                try
                {
                    _allTickets = await _service.GetAllTicketsAsync();
                    if (MainWindow.Instance != null) MainWindow.Instance.AddLog($"✅ Data Loaded. Total: {_allTickets.Count}");
                }
                catch (Exception serviceEx)
                {
                    // 🔥 CAPTURE SERVICE ERROR 🔥
                    CrashLogger.Log(serviceEx, "HISTORY_LOAD_SERVICE", "CRITICAL");
                    MainWindow.Instance?.AddLog($"❌ Service error loading tickets: {serviceEx.Message}");
                    MainWindow.Instance?.AddLog($"Stack Trace: {serviceEx.StackTrace}");
                    throw; // Re-throw to outer catch
                }

                try
                {
                    // 🔥 UPDATE 1: Populate Filter Region Otomatis 🔥
                    PopulateRegionFilter();
                }
                catch (Exception filterEx)
                {
                    CrashLogger.Log(filterEx, "HISTORY_POPULATE_FILTER", "ERROR");
                    MainWindow.Instance?.AddLog($"⚠️ Filter population error: {filterEx.Message}");
                    // Don't re-throw, continue with rest
                }

                try
                {
                    ApplyFilter("");
                }
                catch (Exception filterApplyEx)
                {
                    CrashLogger.Log(filterApplyEx, "HISTORY_APPLY_FILTER", "ERROR");
                    MainWindow.Instance?.AddLog($"⚠️ Apply filter error: {filterApplyEx.Message}");
                }

                try
                {
                    UpdateStatsCards();
                }
                catch (Exception statsEx)
                {
                    CrashLogger.Log(statsEx, "HISTORY_UPDATE_STATS", "ERROR");
                    MainWindow.Instance?.AddLog($"⚠️ Stats update error: {statsEx.Message}");
                }
            }
            catch (Exception ex)
            {
                // 🔥 CAPTURE MAIN LOAD ERROR 🔥
                CrashLogger.Log(ex, "HISTORY_PAGE_LOAD_DATA", "CRITICAL");
                if (MainWindow.Instance != null) MainWindow.Instance.AddLog($"❌ ERROR LOADING DATA: {ex.Message}");
                if (MainWindow.Instance != null) MainWindow.Instance.AddLog($"Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                if (LoadingRing != null) LoadingRing.IsActive = false;
            }
        }

        // 🔥 HELPER BARU: POPULATE REGION 🔥
        private void PopulateRegionFilter()
        {
            try
            {
                if (FilterRegion == null)
                {
                    CrashLogger.LogInfo("FilterRegion not initialized", "PopulateRegionFilter");
                    return;
                }

                // Simpan pilihan user saat ini (kalau ada)
                string currentSelection = (FilterRegion.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ALL";

                // Ambil daftar region unik dari database
                var regions = _allTickets
                    .Select(t => t.Region)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();

                FilterRegion.Items.Clear();

                // Item Default "All Regions"
                var allItem = new ComboBoxItem { Content = "All Regions", Tag = "ALL" };
                FilterRegion.Items.Add(allItem);

                // Tambahkan region yang ditemukan
                foreach (var region in regions)
                {
                    FilterRegion.Items.Add(new ComboBoxItem { Content = region, Tag = region });
                }

                // Restore selection atau default ke ALL
                if (currentSelection == "ALL")
                {
                    FilterRegion.SelectedItem = allItem;
                }
                else
                {
                    var itemToSelect = FilterRegion.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(i => i.Tag.ToString() == currentSelection);
                    FilterRegion.SelectedItem = itemToSelect ?? allItem;
                }

                MainWindow.Instance?.AddLog($"✅ Region filter populated: {FilterRegion.Items.Count} items");
            }
            catch (Exception ex)
            {
                // 🔥 CAPTURE REGION FILTER ERROR 🔥
                CrashLogger.Log(ex, "HISTORY_POPULATE_REGION_FILTER", "ERROR");
                MainWindow.Instance?.AddLog($"❌ PopulateRegionFilter error: {ex.Message}");
                MainWindow.Instance?.AddLog($"Stack Trace: {ex.StackTrace}");
            }
        }

        // --- BUTTON ACTIONS ---

        private void BtnOpenDetail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is HyperlinkButton btn && btn.Tag is TicketLog ticket)
                {
                    Frame.Navigate(typeof(TicketDetailPage), ticket);
                }
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "HISTORY_PAGE_OPEN_DETAIL");
            }
        }

        private async void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_allTickets.Count == 0) return;

                ContentDialog dialog = new ContentDialog
                {
                    Title = "⚠️ HAPUS DATABASE?",
                    Content = $"Anda akan menghapus permanen {_allTickets.Count:N0} tiket.\nProses ini tidak dapat dikembalikan.",
                    PrimaryButtonText = "HAPUS SEMUA",
                    CloseButtonText = "Batal",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.Content.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    if (LoadingRing != null) LoadingRing.IsActive = false;

                    _cts = new CancellationTokenSource();
                    var mainWindow = MainWindow.Instance;

                    if (mainWindow != null)
                    {
                        mainWindow.OnCancelRequested += OnCancelTriggered;
                        mainWindow.ShowProgress(true, "MENGHAPUS DATABASE...");
                        mainWindow.AddLog("🗑️ Starting Batch Deletion...");
                    }

                    if (HistoryDataGrid != null) HistoryDataGrid.IsEnabled = false;

                    var ticketsToDelete = _allTickets.ToList();
                    int total = ticketsToDelete.Count;
                    int current = 0;

                    foreach (var t in ticketsToDelete)
                    {
                        if (_cts.Token.IsCancellationRequested) break;

                        try
                        {
                            await _service.DeleteTicketAsync(t.TT_IOH);
                            current++;

                            if (mainWindow != null)
                            {
                                double percent = ((double)current / total) * 100;
                                mainWindow.UpdateProgress(percent, current, total);
                            }
                        }
                        catch (Exception deleteEx)
                        {
                            // 🔥 CAPTURE DELETE ERROR 🔥
                            CrashLogger.Log(deleteEx, $"HISTORY_DELETE_ITEM_{t.TT_IOH}", "ERROR");
                            if (mainWindow != null) mainWindow.AddLog($"❌ Error deleting {t.TT_IOH}: {deleteEx.Message}");
                            current++;
                        }
                    }

                    _allTickets.Clear();

                    if (_cts.Token.IsCancellationRequested)
                    {
                        if (mainWindow != null) mainWindow.AddLog("🛑 DELETION CANCELLED.");
                        _allTickets = await _service.GetAllTicketsAsync();
                    }
                    else
                    {
                        if (mainWindow != null) mainWindow.AddLog("✅ DATABASE CLEARED.");
                    }

                    FilteredTickets.Clear();
                    // Reset dropdown region karena data kosong
                    PopulateRegionFilter();
                    ApplyFilter("");
                    UpdateStatsCards();
                }
            }
            catch (Exception ex)
            {
                // 🔥 CAPTURE MAIN ERROR 🔥
                CrashLogger.Log(ex, "HISTORY_PAGE_DELETE_ALL", "CRITICAL");
                MainWindow.Instance?.AddLog($"❌ DELETE ERROR: {ex.Message}");
                MainWindow.Instance?.AddLog($"Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                if (HistoryDataGrid != null) HistoryDataGrid.IsEnabled = true;
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.ShowProgress(false);
                    MainWindow.Instance.OnCancelRequested -= OnCancelTriggered;
                }
                if (_cts != null) { _cts.Dispose(); _cts = null; }
            }
        }

        private void OnCancelTriggered()
        {
            _cts?.Cancel();
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnImport.IsEnabled = false;
                if (LoadingRing != null) LoadingRing.IsActive = false;

                var excelService = new ExcelService();
                if (MainWindow.Instance != null) MainWindow.Instance.AddLog("📂 Membaca file Excel...");

                var newTickets = await excelService.ImportTicketsFromExcel();

                if (newTickets.Count > 0)
                {
                    _cts = new CancellationTokenSource();
                    var mainWindow = MainWindow.Instance;

                    if (mainWindow != null)
                    {
                        mainWindow.OnCancelRequested += OnCancelTriggered;
                        mainWindow.ShowProgress(true, "IMPORTING DATA...");
                        mainWindow.AddLog($"📥 Mulai Import {newTickets.Count} data...");
                    }

                    if (HistoryDataGrid != null) HistoryDataGrid.IsEnabled = false;

                    int total = newTickets.Count;
                    int current = 0;
                    int successCount = 0;
                    int errorCount = 0;

                    foreach (var ticket in newTickets)
                    {
                        if (_cts.Token.IsCancellationRequested) break;

                        try
                        {
                            bool isDuplicate = _allTickets.Any(existing =>
                                existing.TT_IOH.Trim().Equals(ticket.TT_IOH.Trim(), StringComparison.OrdinalIgnoreCase));

                            if (!isDuplicate)
                            {
                                await _service.AddTicketAsync(ticket);
                                _allTickets.Add(ticket);
                                successCount++;
                            }
                            else
                            {
                                // 🔥 LOG DUPLICATE 🔥
                                if (mainWindow != null) mainWindow.AddLog($"⚠️ Duplicate ticket skipped: {ticket.TT_IOH}");
                            }
                        }
                        catch (Exception addEx)
                        {
                            // 🔥 CAPTURE INDIVIDUAL ADD ERROR 🔥
                            CrashLogger.Log(addEx, $"HISTORY_IMPORT_ITEM_{ticket.TT_IOH}", "ERROR");
                            if (mainWindow != null) mainWindow.AddLog($"❌ Error importing {ticket.TT_IOH}: {addEx.Message}");
                            errorCount++;
                        }

                        current++;

                        if (mainWindow != null)
                        {
                            double percent = ((double)current / total) * 100;
                            mainWindow.UpdateProgress(percent, current, total);
                        }
                    }

                    if (_cts.Token.IsCancellationRequested)
                    {
                        if (mainWindow != null) mainWindow.AddLog($"🛑 IMPORT DIBATALKAN. ({successCount} berhasil, {errorCount} error)");
                    }
                    else
                    {
                        if (mainWindow != null) mainWindow.AddLog($"✅ IMPORT SELESAI. {successCount} Tiket baru. {(errorCount > 0 ? $"{errorCount} error(s)" : "Semua OK") }");
                    }

                    // 🔥 REFRESH REGION DROP DOWN 🔥
                    PopulateRegionFilter();
                    ApplyFilter("");
                    UpdateStatsCards();
                }
                else
                {
                    if (MainWindow.Instance != null) MainWindow.Instance.AddLog("⚠️ Tidak ada data ditemukan di file Excel.");
                }
            }
            catch (Exception ex)
            {
                // 🔥 CAPTURE MAIN IMPORT ERROR 🔥
                CrashLogger.Log(ex, "HISTORY_PAGE_IMPORT", "CRITICAL");
                if (MainWindow.Instance != null) MainWindow.Instance.AddLog($"❌ IMPORT ERROR: {ex.Message}");
                if (MainWindow.Instance != null) MainWindow.Instance.AddLog($"Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                BtnImport.IsEnabled = true;
                if (HistoryDataGrid != null) HistoryDataGrid.IsEnabled = true;
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.ShowProgress(false);
                    MainWindow.Instance.OnCancelRequested -= OnCancelTriggered;
                }
                if (_cts != null) { _cts.Dispose(); _cts = null; }
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnExport.IsEnabled = false;
                var excelService = new ExcelService();
                bool result = await excelService.ExportTicketsToExcel(FilteredTickets.ToList());
                if (result && MainWindow.Instance != null) MainWindow.Instance.AddLog("✅ Export Excel Berhasil.");
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "HISTORY_PAGE_EXPORT");
                if (MainWindow.Instance != null) MainWindow.Instance.AddLog($"❌ Export Error: {ex.Message}");
            }
            finally { BtnExport.IsEnabled = true; }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button b && b.Tag is TicketLog t)
                {
                    ContentDialog d = new ContentDialog
                    {
                        Title = "Hapus Tiket?",
                        Content = $"Hapus riwayat {t.TT_IOH}?",
                        PrimaryButtonText = "Hapus",
                        CloseButtonText = "Batal",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.Content.XamlRoot
                    };

                    if (await d.ShowAsync() == ContentDialogResult.Primary)
                    {
                        await _service.DeleteTicketAsync(t.TT_IOH);
                        _allTickets.Remove(t);
                        // Refresh dropdown kalau-kalau region itu hilang
                        PopulateRegionFilter();
                        ApplyFilter(SearchBox?.Text ?? "");
                        UpdateStatsCards();
                    }
                }
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "HISTORY_PAGE_DELETE_ITEM");
            }
        }

        // --- SORTING & FILTER ---

        private void HistoryDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
        {
            try
            {
                if (e.Column.Tag == null) return;
                string tag = e.Column.Tag.ToString();

                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                    SortData(tag, true);
                }
                else
                {
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                    SortData(tag, false);
                }

                foreach (var col in HistoryDataGrid.Columns)
                {
                    if (col.Tag?.ToString() != tag) col.SortDirection = null;
                }
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "HISTORY_PAGE_SORTING");
            }
        }

        private void SortData(string sortBy, bool ascending)
        {
            try
            {
                var currentData = FilteredTickets.ToList();
                List<TicketLog> sortedData;

                switch (sortBy)
                {
                    case "TT_IOH":
                        sortedData = ascending ? currentData.OrderBy(t => t.TT_IOH).ToList() : currentData.OrderByDescending(t => t.TT_IOH).ToList();
                        break;
                    // 🔥 UPDATE 2: SORTING REGION 🔥
                    case "Region":
                        sortedData = ascending ? currentData.OrderBy(t => t.Region).ToList() : currentData.OrderByDescending(t => t.Region).ToList();
                        break;
                    case "SegmentPM":
                        sortedData = ascending ? currentData.OrderBy(t => t.SegmentPM).ToList() : currentData.OrderByDescending(t => t.SegmentPM).ToList();
                        break;
                    case "CutPoint":
                        sortedData = ascending ? currentData.OrderBy(t => t.CutPoint).ToList() : currentData.OrderByDescending(t => t.CutPoint).ToList();
                        break;
                    case "Status":
                        sortedData = ascending ? currentData.OrderBy(t => t.Status).ToList() : currentData.OrderByDescending(t => t.Status).ToList();
                        break;
                    case "OccurTime":
                        sortedData = ascending
                            ? currentData.OrderBy(t => ParseDate(t.OccurTime)).ToList()
                            : currentData.OrderByDescending(t => ParseDate(t.OccurTime)).ToList();
                        break;
                    case "RootCause":
                        sortedData = ascending ? currentData.OrderBy(t => t.RootCause).ToList() : currentData.OrderByDescending(t => t.RootCause).ToList();
                        break;
                    default:
                        return;
                }

                FilteredTickets.Clear();
                foreach (var item in sortedData) FilteredTickets.Add(item);
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "HISTORY_PAGE_SORT_DATA");
            }
        }

        private DateTime ParseDate(string dateStr)
        {
            if (DateTime.TryParse(dateStr, out DateTime dt)) return dt;
            return DateTime.MinValue;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) => SafeApplyFilter(sender.Text);
        private void FilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => SafeApplyFilter(SearchBox?.Text ?? "");
        private void FilterDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) => SafeApplyFilter(SearchBox?.Text ?? "");

        // 🔥 UPDATE 3: EVENT HANDLER FILTER REGION 🔥
        private void FilterRegion_SelectionChanged(object sender, SelectionChangedEventArgs e) => SafeApplyFilter(SearchBox?.Text ?? "");

        // 🔥 NEW: STAT CARD HOVER EFFECTS 🔥
        private void StatCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border card)
            {
                // Elevate card dengan scale animation
                var scaleAnimation = new Windows.UI.Composition.Compositor().CreateVector3KeyFrameAnimation();
                card.Translation = new System.Numerics.Vector3(0, -8, 20);
            }
        }

        private void StatCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border card)
            {
                // Return to normal state
                card.Translation = new System.Numerics.Vector3(0, 0, 12);
            }
        }

        private void SafeApplyFilter(string query)
        {
            try
            {
                ApplyFilter(query);
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "HISTORY_PAGE_FILTER");
            }
        }

        private void ApplyFilter(string query)
        {
            if (_allTickets == null) return;
            if (HistoryDataGrid == null) return; // Add check for HistoryDataGrid

            var filtered = _allTickets.ToList();

            // 1. FILTER PENCARIAN
            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                filtered = filtered.Where(x =>
                    (x.TT_IOH?.ToLower().Contains(query) ?? false) ||
                    (x.SegmentPM?.ToLower().Contains(query) ?? false) ||
                    (x.RootCause?.ToLower().Contains(query) ?? false) ||
                    (x.Region?.ToLower().Contains(query) ?? false) // Tambah search region
                ).ToList();
            }

            // 2. FILTER STATUS
            if (FilterStatus != null && FilterStatus.SelectedItem is ComboBoxItem statusItem && statusItem.Tag?.ToString() != "ALL")
            {
                string statusTag = statusItem.Tag.ToString(); // DOWN, UP, atau CANCEL

                // Logic filter yang lebih robust
                filtered = filtered.Where(x =>
                    x.Status != null &&
                    x.Status.ToUpper().Contains(statusTag)
                ).ToList();
            }

            // 🔥 3. FILTER REGION (LOGIC BARU) 🔥
            if (FilterRegion != null && FilterRegion.SelectedItem is ComboBoxItem regionItem && regionItem.Tag?.ToString() != "ALL")
            {
                string selectedRegion = regionItem.Tag.ToString();
                filtered = filtered.Where(x => x.Region != null && x.Region == selectedRegion).ToList();
            }

            // 4. FILTER TANGGAL
            if (FilterDate != null && FilterDate.Date != null)
            {
                string dateStr = FilterDate.Date.Value.ToString("dd/MM/yyyy");
                filtered = filtered.Where(x => x.DispatchTime != null && x.DispatchTime.Contains(dateStr)).ToList();
            }

            FilteredTickets.Clear();
            foreach (var ticket in filtered) FilteredTickets.Add(ticket);
            foreach (var col in HistoryDataGrid.Columns) col.SortDirection = null;

            HistoryDataGrid.ItemsSource = FilteredTickets;
            if (EmptyState != null) EmptyState.Visibility = FilteredTickets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            UpdateStatsCards();
        }

        private void UpdateStatsCards()
        {
            try
            {
                if (TxtTotalCount == null) return;
                TxtTotalCount.Text = _allTickets.Count.ToString("N0");
                TxtOpenCount.Text = _allTickets.Count(x => x.Status != null && x.Status.ToUpper().Contains("DOWN")).ToString("N0");
                TxtClosedCount.Text = _allTickets.Count(x => x.Status != null && (x.Status.ToUpper().Contains("UP") || x.Status.ToUpper().Contains("RESOLVE"))).ToString("N0");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Stats Error: " + ex.Message);
            }
        }
    }
}