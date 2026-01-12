using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SMART_NOC.Services;
using SMART_NOC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Web.WebView2.Core;
using Windows.Storage;
using System.Text;
using System.Net; // Wajib untuk WebUtility.HtmlEncode

// Mengatasi Ambiguous Reference
using Page = Microsoft.UI.Xaml.Controls.Page;

namespace SMART_NOC.Views
{
    public sealed partial class LiveMapPage : Page
    {
        private HistoryService _historyService = new HistoryService();
        private List<TicketLog> _allTickets = new List<TicketLog>();

        public LiveMapPage()
        {
            this.InitializeComponent();

            this.Loaded += LiveMapPage_Loaded;

            // Listener komunikasi JS <-> C#
            MapWebView.WebMessageReceived += MapWebView_WebMessageReceived;
            
            // 🔥 NEW: Register untuk menerima notifikasi refresh dari halaman lain 🔥
            if (MainWindow.Instance != null)
            {
                MainWindow.Instance.MapPageInstance = this;
            }
        }

        private void DebugLog(string msg)
        {
            if (MainWindow.Instance != null) MainWindow.Instance.AddLog(msg);
        }

        /// <summary>
        /// Announce accessibility message to screen readers for dynamic content updates
        /// </summary>
        private void AnnounceToScreenReader(string message)
        {
            DebugLog($"[A11Y] 📢 Screen reader announcement: {message}");
            // Future: Integrate with WinUI 3 LiveRegion when available
            // For now, this is tracked in debug log
        }

        private async void LiveMapPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= LiveMapPage_Loaded;
            AnnounceToScreenReader("Live Map page loaded. Map initializing...");
            await InitializeMapAsync();
        }

        // --- 🔥 NEW: PUBLIC METHOD UNTUK REFRESH MAP 🔥 ---
        public async Task RefreshMapData()
        {
            try
            {
                DebugLog("[MAP] 🔄 RefreshMapData called - Reloading tickets from database...");
                AnnounceToScreenReader("Map data refreshing. Please wait for update...");
                await LoadAndInjectData();
                DebugLog("[MAP] ✅ RefreshMapData completed successfully!");
                AnnounceToScreenReader($"Map updated. {(TxtTotalMarkers != null ? TxtTotalMarkers.Text : "Incidents displayed")}");
            }
            catch (Exception ex)
            {
                DebugLog($"[MAP] ❌ RefreshMapData failed: {ex.Message}");
                AnnounceToScreenReader($"Error updating map: {ex.Message}");
                CrashLogger.Log(ex, "LiveMapPage_RefreshMapData", "ERROR");
            }
        }

        // --- 1. INISIALISASI PETA ---
        private async Task InitializeMapAsync()
        {
            UpdateLoadingState(true, "MEMUAT SYSTEM PETA...", "Inisialisasi Core WebView2...");

            try
            {
                // Gunakan Default Environment agar stabil
                await MapWebView.EnsureCoreWebView2Async();

                // Load HTML Skeleton
                string skeletonHtml = GetMapSkeletonHtml();
                MapWebView.NavigateToString(skeletonHtml);
            }
            catch (Exception ex)
            {
                DebugLog($"[MAP] Init Failed: {ex.Message}");
                if (!ex.Message.Contains("initialized"))
                {
                    UpdateLoadingState(false);
                    if (TxtTotalMarkers != null) TxtTotalMarkers.Text = "Gagal memuat peta.";
                }
            }
        }

        // --- 2. KOMUNIKASI DATA (C# <-> JS) ---
        private async void MapWebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                string message = args.TryGetWebMessageAsString();

                if (message == "MAP_READY")
                {
                    DebugLog("[MAP] Skeleton Ready. Fetching Data...");
                    await LoadAndInjectData();
                }
                else if (message == "RENDER_COMPLETE")
                {
                    DebugLog("[MAP] Visual Rendering Complete!");
                    UpdateLoadingState(false);
                }
            }
            catch (Exception ex)
            {
                DebugLog($"[MAP] Msg Error: {ex.Message}");
            }
        }

        private async Task LoadAndInjectData()
        {
            UpdateLoadingState(true, "MEMINDAI DATA...", "Mengunduh tiket dari database...");

            try
            {
                _allTickets = await _historyService.GetAllTicketsAsync();
                if (_allTickets == null) _allTickets = new List<TicketLog>();

                // Langsung jalankan filter default (Show All)
                await ApplyFilters();
            }
            catch (Exception ex)
            {
                DebugLog($"[MAP] Data Error: {ex.Message}");
                UpdateLoadingState(false);
            }
        }

        // --- 3. FITUR AUTO SUGGEST ---
        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && _allTickets.Count > 0)
            {
                string query = sender.Text.ToLower();
                if (string.IsNullOrWhiteSpace(query))
                {
                    sender.ItemsSource = null;
                    return;
                }

                // 🔥 IMPROVED: Cari berdasarkan Ticket ID & Segment PM 🔥
                var suggestions = _allTickets
                    .Where(t => 
                        (t.TT_IOH != null && t.TT_IOH.ToLower().Contains(query)) ||
                        (t.SegmentPM != null && t.SegmentPM.ToLower().Contains(query))
                    )
                    .GroupBy(t => new { t.TT_IOH, t.SegmentPM })
                    .Select(g => $"{g.Key.TT_IOH} • {g.Key.SegmentPM}")
                    .Distinct()
                    .Take(12)
                    .ToList();

                if (suggestions.Count == 0)
                {
                    // Fallback: cari hanya by segment
                    suggestions = _allTickets
                        .Where(t => t.SegmentPM != null && t.SegmentPM.ToLower().Contains(query))
                        .Select(t => t.SegmentPM)
                        .Distinct()
                        .Take(12)
                        .ToList();
                }

                sender.ItemsSource = suggestions;
            }
        }

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await ApplyFilters();
        }

        // --- 4. HANDLER TOMBOL ---
        private async void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            await ApplyFilters();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            CmbRegion.SelectedIndex = 0;
            CmbStatus.SelectedIndex = 0;
            DateStart.Date = null;
            DateEnd.Date = null;
            _ = ApplyFilters();
        }

        // 🔥 NEW: EVENT HANDLER FOR REGION DROPDOWN - AUTO APPLY FILTER 🔥
        private async void CmbRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ApplyFilters();
        }

        // --- 5. LOGIC FILTER & GROUPING ---
        private async Task ApplyFilters()
        {
            UpdateLoadingState(true, "MEMFILTER DATA...", "Menerapkan kriteria & grouping...");

            try
            {
                // Ambil nilai UI di thread utama
                string searchQuery = SearchBox.Text?.Trim().ToUpper() ?? "";
                
                // 🔥 NEW: REGION FILTER 🔥
                string regionTag = "ALL";
                if (CmbRegion.SelectedItem is ComboBoxItem regionItem && regionItem.Tag != null)
                {
                    regionTag = regionItem.Tag.ToString();
                }
                
                string statusTag = "ALL";
                if (CmbStatus.SelectedItem is ComboBoxItem statusItem && statusItem.Tag != null)
                {
                    statusTag = statusItem.Tag.ToString();
                }

                DateTime? dStart = DateStart.Date?.Date;
                DateTime? dEnd = DateEnd.Date?.Date;

                var markersData = await Task.Run(() =>
                {
                    // 1. Ambil Data
                    IEnumerable<TicketLog> queryData = _allTickets;

                    // 2. Filter Region (Case Insensitive) 🔥 NEW 🔥
                    if (regionTag != "ALL")
                    {
                        queryData = queryData.Where(t =>
                            t.Region != null && t.Region.Equals(regionTag, StringComparison.OrdinalIgnoreCase)
                        );
                    }

                    // 3. Filter Search Text (Case Insensitive)
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        queryData = queryData.Where(t =>
                            (t.TT_IOH != null && t.TT_IOH.ToUpper().Contains(searchQuery)) ||
                            (t.SegmentPM != null && t.SegmentPM.ToUpper().Contains(searchQuery))
                        );
                    }

                    // 4. Filter Status (Case Insensitive & Robust)
                    if (statusTag == "DOWN")
                    {
                        queryData = queryData.Where(t => t.Status != null && t.Status.ToUpper().Contains("DOWN"));
                    }
                    else if (statusTag == "UP")
                    {
                        queryData = queryData.Where(t => t.Status != null &&
                            (t.Status.ToUpper().Contains("UP") ||
                             t.Status.ToUpper().Contains("CLOSE") ||
                             t.Status.ToUpper().Contains("RESOLVE")));
                    }

                    // 5. Filter Tanggal
                    if (dStart.HasValue)
                    {
                        queryData = queryData.Where(t => ParseDateSafe(t.OccurTime) >= dStart.Value);
                    }
                    if (dEnd.HasValue)
                    {
                        DateTime endOfDay = dEnd.Value.AddDays(1).AddTicks(-1);
                        queryData = queryData.Where(t => ParseDateSafe(t.OccurTime) <= endOfDay);
                    }

                    // 6. GROUPING & VALIDASI (Hanya ambil yang punya koordinat valid)
                    var groupedByLoc = queryData
                        .Where(t => !string.IsNullOrEmpty(t.CutPoint) && ExtractLatLong(t.CutPoint) != null)
                        .GroupBy(t => t.CutPoint);

                    return groupedByLoc.Select(grp =>
                    {
                        var tickets = grp.ToList();
                        var count = tickets.Count;
                        var latLong = ExtractLatLong(grp.Key!); // Method ini aman karena sudah difilter di atas

                        // Cek status grup (Merah jika ada min. 1 tiket DOWN)
                        bool isGroupDown = tickets.Any(t => t.Status != null && t.Status.ToUpper().Contains("DOWN"));

                        string popupHtml = (count == 1)
                            ? GeneratePopupHtml(tickets[0], isGroupDown, latLong!.Item1, latLong.Item2)
                            : GenerateMultiPopupHtml(tickets, isGroupDown, latLong!.Item1, latLong.Item2);

                        return new
                        {
                            lat = latLong!.Item1,
                            lng = latLong.Item2,
                            count = count,
                            desc = popupHtml,
                            statusType = isGroupDown ? "down" : "up"
                        };
                    }).ToList();
                });

                // Update UI Text
                if (TxtTotalMarkers != null)
                    TxtTotalMarkers.Text = $"Menampilkan {markersData.Count} Lokasi ({markersData.Sum(m => m.count)} Tiket).";

                // Kirim Data ke WebView
                string jsonString = JsonConvert.SerializeObject(markersData);
                MapWebView.CoreWebView2.PostWebMessageAsJson(jsonString);

                UpdateLoadingState(false);
            }
            catch (Exception ex)
            {
                DebugLog($"[FILTER ERROR]: {ex.Message}");
                UpdateLoadingState(false);
            }
        }

        // --- 6. HTML GENERATORS ---

        // Popup untuk 1 Tiket
        private string GeneratePopupHtml(TicketLog t, bool isDown, double lat, double lng)
        {
            string color = isDown ? "#ff3b30" : "#34c759";
            string gmaps = $"http://maps.google.com/?q={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            string duration = CalculateDuration(t);

            // Encode teks untuk keamanan HTML
            string safeId = WebUtility.HtmlEncode(t.TT_IOH ?? "-");
            string safeSeg = WebUtility.HtmlEncode(t.SegmentPM ?? "-");
            string safeStatus = WebUtility.HtmlEncode(t.Status ?? "-");
            string safeRegion = WebUtility.HtmlEncode(t.Region ?? "-");
            string safeRootCause = WebUtility.HtmlEncode(t.RootCause ?? "-");

            return $@"<div style='font-family: Segoe UI; min-width:280px; color:#333;'>
                        <div style='background:{color}; color:white; padding:10px 12px; border-radius:6px 6px 0 0; display:flex; justify-content:space-between; align-items:center;'>
                            <strong style='font-size:13px;'>{safeId}</strong>
                            <span style='background:rgba(255,255,255,0.2); font-size:10px; padding:3px 6px; border-radius:4px;'>{duration}</span>
                        </div>
                        <div style='padding:14px; border:1px solid #ddd; border-top:none; border-radius:0 0 6px 6px; background:white;'>
                            <div style='font-size:10px; color:#999; font-weight:bold; text-transform:uppercase; margin-bottom:10px;'>DETAILS</div>
                            
                            <div style='margin-bottom:8px;'>
                                <div style='font-size:10px; color:#888;'>REGION</div>
                                <div style='font-size:12px; font-weight:500; color:#0078d4;'>{safeRegion}</div>
                            </div>
                            
                            <div style='margin-bottom:8px;'>
                                <div style='font-size:10px; color:#888;'>SEGMENT</div>
                                <div style='font-size:12px; font-weight:500;'>{safeSeg}</div>
                            </div>
                            
                            <div style='display:grid; grid-template-columns:1fr 1fr; gap:8px; margin-bottom:10px;'>
                                <div>
                                    <div style='font-size:10px; color:#888;'>STATUS</div>
                                    <div style='font-weight:bold; color:{color}; font-size:12px;'>{safeStatus}</div>
                                </div>
                                <div>
                                    <div style='font-size:10px; color:#888;'>ROOT CAUSE</div>
                                    <div style='font-size:11px; background:#f0f0f0; padding:3px 4px; border-radius:3px; overflow:hidden; text-overflow:ellipsis; max-height:30px;'>{safeRootCause}</div>
                                </div>
                            </div>
                            
                            <div style='margin-bottom:10px; padding:8px; background:#f5f5f5; border-radius:4px; border-left:3px solid {color};'>
                                <div style='font-size:10px; color:#666;'>📍 Coordinates</div>
                                <div style='font-size:11px; font-family:Consolas; color:#333; font-weight:bold;'>{lat:F4}, {lng:F4}</div>
                            </div>
                            
                            <a href='{gmaps}' target='_blank' style='display:block; text-align:center; background:#0078d4; color:white; text-decoration:none; padding:9px; border-radius:4px; font-size:12px; font-weight:bold; cursor:pointer;'>📍 Open Google Maps</a>
                        </div>
                      </div>";
        }

        // Popup untuk Banyak Tiket (List) dengan Header Cut Point
        private string GenerateMultiPopupHtml(List<TicketLog> tickets, bool isGroupDown, double lat, double lng)
        {
            string headerColor = isGroupDown ? "#ff3b30" : "#34c759";
            string gmaps = $"http://maps.google.com/?q={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            // 🔥 AMBIL NAMA CUT POINT (Header) 🔥
            string rawHeaderTitle = tickets.FirstOrDefault()?.CutPoint ?? "Unknown Location";
            string safeHeaderTitle = WebUtility.HtmlEncode(rawHeaderTitle);
            
            // 🔥 REGION INFO DARI TICKET PERTAMA 🔥
            string headerRegion = WebUtility.HtmlEncode(tickets.FirstOrDefault()?.Region ?? "-");
            
            // HITUNG STATISTICS
            int downCount = tickets.Count(t => t.Status != null && t.Status.ToUpper().Contains("DOWN"));
            int upCount = tickets.Count - downCount;

            StringBuilder sb = new StringBuilder();

            sb.Append($@"<div style='font-family: Segoe UI; min-width:320px; color:#333;'>
                        <div style='background:{headerColor}; color:white; padding:12px; border-radius:6px 6px 0 0;'>
                            <div style='display:flex; justify-content:space-between; align-items:flex-start; margin-bottom:8px;'>
                                <div>
                                    <strong style='font-size:14px; display:block;'>{tickets.Count} Tiket</strong>
                                    <div style='font-size:11px; opacity:0.95; margin-top:3px; line-height:1.3;'>{safeHeaderTitle}</div>
                                </div>
                                <a href='{gmaps}' target='_blank' style='color:white; font-size:11px; text-decoration:underline; white-space:nowrap;'>Maps ↗</a>
                            </div>
                            
                            <div style='font-size:10px; opacity:0.85; padding-top:8px; border-top:1px solid rgba(255,255,255,0.3); display:grid; grid-template-columns:1fr 1fr; gap:8px;'>
                                <div>🔴 DOWN: <strong>{downCount}</strong></div>
                                <div>🟢 UP: <strong>{upCount}</strong></div>
                            </div>
                            
                            <div style='font-size:10px; opacity:0.85; margin-top:6px;'>📍 Region: <strong>{headerRegion}</strong></div>
                        </div>
                        
                        <div style='max-height:300px; overflow-y:auto; border:1px solid #ddd; border-top:none; background:white;'>");

            foreach (var t in tickets)
            {
                bool isItemDown = t.Status != null && t.Status.ToUpper().Contains("DOWN");
                string itemColor = isItemDown ? "#ff3b30" : "#34c759";
                string duration = CalculateDuration(t);

                string safeId = WebUtility.HtmlEncode(t.TT_IOH ?? "-");
                string safeSeg = WebUtility.HtmlEncode(t.SegmentPM ?? "-");
                string safeStatus = WebUtility.HtmlEncode(t.Status ?? "-");
                string safeRootCause = WebUtility.HtmlEncode(t.RootCause ?? "-");

                sb.Append($@"<div style='padding:11px; border-bottom:1px solid #eee; background:#fafafa;'>
                                <div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:4px;'>
                                    <strong style='color:{itemColor}; font-size:12px;'>{safeId}</strong>
                                    <span style='font-size:9px; background:white; padding:2px 4px; border-radius:3px; border:1px solid #ddd;'>{duration}</span>
                                </div>
                                <div style='font-size:11px; color:#555; margin-bottom:2px;'>{safeSeg}</div>
                                <div style='font-size:10px; color:#999;'>{safeStatus}</div>
                                {(string.IsNullOrEmpty(safeRootCause) || safeRootCause == "-" ? "" : $"<div style='font-size:9px; color:#666; margin-top:2px; padding:2px 0;'>Root: <em>{safeRootCause}</em></div>")}
                             </div>");
            }

            sb.Append("</div></div>");
            return sb.ToString();
        }

        private string CalculateDuration(TicketLog t)
        {
            if (DateTime.TryParse(t.OccurTime, out DateTime occ))
            {
                TimeSpan span = TimeSpan.Zero;

                // Fallback Logic: Cek Status jika ResolvedDate tidak ada
                if (t.Status != null && (t.Status.Contains("CLOSE") || t.Status.Contains("UP") || t.Status.Contains("RESOLVE")))
                {
                    return "RESOLVED";
                }
                else
                {
                    span = DateTime.Now - occ;
                }

                if (span.TotalSeconds > 0)
                {
                    if (span.TotalDays >= 1) return $"{(int)span.TotalDays}d {span.Hours}h";
                    return $"{(int)span.TotalHours}h {span.Minutes}m";
                }
            }
            return "-";
        }

        // --- 7. HELPER LAIN ---

        private DateTime ParseDateSafe(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return DateTime.MinValue;
            if (DateTime.TryParse(dateStr, out DateTime result)) return result;
            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out result)) return result;
            return DateTime.MinValue;
        }

        private Tuple<double, double>? ExtractLatLong(string cutPoint)
        {
            if (string.IsNullOrEmpty(cutPoint)) return null;
            try
            {
                // 🔥 TRY METHOD 1: Extract dari dalam kurung "(lat, lon)" 🔥
                var match = System.Text.RegularExpressions.Regex.Match(cutPoint, @"\(([^)]+)\)");
                if (match.Success)
                {
                    var coords = match.Groups[1].Value.Trim();
                    
                    // Validasi kurung tidak kosong
                    if (string.IsNullOrWhiteSpace(coords)) return null;
                    
                    var parts = coords.Split(',');
                    if (parts.Length == 2)
                    {
                        string latStr = parts[0].Trim();
                        string lngStr = parts[1].Trim();
                        
                        // Validasi kedua bagian tidak kosong
                        if (string.IsNullOrWhiteSpace(latStr) || string.IsNullOrWhiteSpace(lngStr)) 
                            return null;
                        
                        if (double.TryParse(latStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                            double.TryParse(lngStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lng))
                        {
                            // Validasi range koordinat Indonesia (lat: -11 to 6, lon: 95 to 141)
                            if (lat >= -11 && lat <= 6 && lng >= 95 && lng <= 141)
                            {
                                return Tuple.Create(lat, lng);
                            }
                        }
                    }
                }

                // 🔥 TRY METHOD 2: Direct parsing (fallback) 🔥
                var rawParts = cutPoint.Split(',');
                if (rawParts.Length >= 2)
                {
                    string latStr = rawParts[0].Trim();
                    string lngStr = rawParts[1].Trim();
                    
                    // Remove any trailing parentheses
                    latStr = System.Text.RegularExpressions.Regex.Replace(latStr, @"[()]+", "").Trim();
                    lngStr = System.Text.RegularExpressions.Regex.Replace(lngStr, @"[()]+", "").Trim();
                    
                    if (string.IsNullOrWhiteSpace(latStr) || string.IsNullOrWhiteSpace(lngStr))
                        return null;
                    
                    if (double.TryParse(latStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse(lngStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lng))
                    {
                        // Validasi range koordinat Indonesia
                        if (lat >= -11 && lat <= 6 && lng >= 95 && lng <= 141)
                        {
                            return Tuple.Create(lat, lng);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"[COORD PARSE ERROR] {cutPoint}: {ex.Message}");
            }
            return null;
        }

        private void UpdateLoadingState(bool isActive, string title = "", string sub = "")
        {
            if (LoadingOverlay == null) return;
            LoadingOverlay.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
            if (LoadingRing != null) LoadingRing.IsActive = isActive;
            if (isActive)
            {
                if (TxtLoadingTitle != null) TxtLoadingTitle.Text = title;
                if (TxtLoadingSub != null) TxtLoadingSub.Text = sub;
            }
        }

        // --- 8. HTML SKELETON (DENGAN FITUR ADVANCED) ---
        private string GetMapSkeletonHtml()
        {
            return """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8' />
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
                <link rel='stylesheet' href='https://unpkg.com/leaflet.markercluster/dist/MarkerCluster.css' />
                <style>
                    html, body { height: 100%; margin: 0; padding: 0; background: #ffffff; font-family: 'Segoe UI', sans-serif; }
                    #map { width: 100%; height: 100%; }
                    .custom-marker-clean { background: none !important; border: none !important; }
                    .cluster-marker { width: 40px; height: 40px; border-radius: 50%; text-align: center; color: white; font-weight: bold; line-height: 40px; box-shadow: 0 4px 10px rgba(0,0,0,0.3); display: flex; align-items: center; justify-content: center; }
                    .pin-down { background-color: #ff3b30; width: 14px; height: 14px; border-radius: 50%; border: 2px solid white; box-shadow: 0 0 8px rgba(255, 59, 48, 0.6); }
                    .pin-up { background-color: #34c759; width: 14px; height: 14px; border-radius: 50%; border: 2px solid white; box-shadow: 0 0 8px rgba(52, 199, 89, 0.4); }
                    .merged-marker { width: 28px; height: 28px; border-radius: 50%; text-align: center; color: white; font-weight: bold; line-height: 28px; font-size: 12px; border: 2px solid white; box-shadow: 0 2px 6px rgba(0,0,0,0.4); cursor: pointer; pointer-events: auto; }
                    .merged-down { background: #ff3b30; }
                    .merged-up { background: #34c759; }
                    .zoom-indicator { position: absolute; bottom: 40px; left: 10px; background: white; padding: 8px 12px; border-radius: 6px; box-shadow: 0 2px 6px rgba(0,0,0,0.1); font-size: 11px; z-index: 500; }
                </style>
            </head>
            <body>
                <div id='map'></div>
                <div class='zoom-indicator' id='zoomIndicator'>Zoom: 5</div>
                <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
                <script src='https://unpkg.com/leaflet.markercluster/dist/leaflet.markercluster.js'></script>
                <script>
                    var map = L.map('map', { zoomControl: false, preferCanvas: true, attributionControl: false }).setView([-2.5489, 118.0149], 5);
                    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', { maxZoom: 19 }).addTo(map);
                    L.control.zoom({ position: 'bottomright' }).addTo(map);
                    var markers = L.markerClusterGroup({ chunkedLoading: true, spiderfyOnMaxZoom: true, showCoverageOnHover: false, zoomToBoundsOnClick: true, maxClusterRadius: 50, disableClusteringAtZoom: 15, iconCreateFunction: function (cluster) { var children = cluster.getAllChildMarkers(); var downCount = 0; children.forEach(function(m) { if (m.options.statusType === 'down') downCount++; }); var realTotal = 0; children.forEach(function(m) { realTotal += (m.options.ticketCount || 1); }); var downPercentage = (downCount / children.length) * 100; var bg = 'conic-gradient(#ff3b30 0% ' + downPercentage + '%, #34c759 ' + downPercentage + '% 100%)'; return L.divIcon({ html: '<div class="cluster-marker" style="background: ' + bg + '; border: 2px solid white;">' + realTotal + '</div>', className: 'custom-cluster', iconSize: L.point(40, 40), iconAnchor: L.point(20, 20) }); } });
                    map.addLayer(markers);
                    map.on('zoomend', function() { document.getElementById('zoomIndicator').innerText = 'Zoom: ' + map.getZoom(); });
                    window.chrome.webview.postMessage('MAP_READY');
                    window.chrome.webview.addEventListener('message', function(evt) { var data = evt.data; if(data) { markers.clearLayers(); renderBatches(data); } });
                    function renderBatches(data) { var batchSize = 300; var index = 0; var total = data.length; function nextBatch() { var end = Math.min(index + batchSize, total); var newLayers = []; for (var i = index; i < end; i++) { var item = data[i]; var icon; if (item.count > 1) { var cssClass = item.statusType === 'down' ? 'merged-down' : 'merged-up'; icon = L.divIcon({ className: 'custom-marker-clean', html: '<div class="merged-marker ' + cssClass + '">' + item.count + '</div>', iconSize: [28, 28], iconAnchor: L.point(14, 14) }); } else { var cssClass = item.statusType === 'down' ? 'pin-down' : 'pin-up'; icon = L.divIcon({ className: 'custom-marker-clean', html: '<div class="' + cssClass + '"></div>', iconSize: [14, 14], iconAnchor: L.point(7, 7) }); } var m = L.marker([item.lat, item.lng], { icon: icon, statusType: item.statusType, ticketCount: item.count }); m.bindPopup(item.desc, { maxWidth: 320, maxHeight: 400 }); m.on('click', function (e) { L.DomEvent.stopPropagation(e); this.openPopup(); }); newLayers.push(m); } markers.addLayers(newLayers); index += batchSize; if (index < total) { requestAnimationFrame(nextBatch); } else { if (total > 0) map.fitBounds(markers.getBounds(), { padding: [50, 50], maxZoom: 13 }); window.chrome.webview.postMessage('RENDER_COMPLETE'); } } requestAnimationFrame(nextBatch); }
                </script>
            </body>
            </html>
            """;
        }
    }
}