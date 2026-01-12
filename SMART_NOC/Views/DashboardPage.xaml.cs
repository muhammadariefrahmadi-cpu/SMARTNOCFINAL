using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SkiaSharp;
using SMART_NOC.Models;
using SMART_NOC.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace SMART_NOC.Views
{
    public sealed partial class DashboardPage : Page
    {
        // Service untuk mengambil data tiket dari database/excel
        private HistoryService _service = new HistoryService();

        // Cache data tiket mentah
        private List<TicketLog> _allData = new List<TicketLog>();

        // Enum untuk mengatur level zoom pada grafik trend
        private enum ChartGranularity { Day, Week, Month, Year }
        private ChartGranularity _currentGranularity = ChartGranularity.Day;

        // Property styling untuk text legend pada chart
        public SolidColorPaint LegendTextPaint { get; set; } = new SolidColorPaint
        {
            Color = SKColors.WhiteSmoke,
            SKTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
        };

        public DashboardPage()
        {
            this.InitializeComponent();

            // Set default range tanggal (1 minggu terakhir)
            StartDatePicker.Date = DateTimeOffset.Now.AddDays(-6);
            EndDatePicker.Date = DateTimeOffset.Now;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Muat data saat halaman pertama kali dibuka
            await LoadDashboardData();
        }

        // --- EVENT HANDLERS (INTERACTION) ---

        private async void Filter_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            // Reset zoom ke harian jika user mengubah tanggal manual
            _currentGranularity = ChartGranularity.Day;
            await LoadDashboardData();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _currentGranularity = ChartGranularity.Day;
            await LoadDashboardData();
        }

        // --- ZOOM BUTTON HANDLERS ---

        private async void BtnZoomDay_Click(object sender, RoutedEventArgs e)
        {
            _currentGranularity = ChartGranularity.Day;
            await LoadDashboardData();
            UpdateZoomButtonState();
        }

        private async void BtnZoomWeek_Click(object sender, RoutedEventArgs e)
        {
            _currentGranularity = ChartGranularity.Week;
            // Extend date range otomatis jika range terlalu pendek untuk view mingguan
            if (StartDatePicker.Date.HasValue && EndDatePicker.Date.HasValue)
            {
                if ((EndDatePicker.Date.Value - StartDatePicker.Date.Value).TotalDays < 7)
                {
                    EndDatePicker.Date = StartDatePicker.Date.Value.AddDays(6);
                }
            }
            await LoadDashboardData();
            UpdateZoomButtonState();
        }

        private async void BtnZoomMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentGranularity = ChartGranularity.Month;
            // Extend date range ke minimal 1 bulan
            if (StartDatePicker.Date.HasValue && EndDatePicker.Date.HasValue)
            {
                if ((EndDatePicker.Date.Value - StartDatePicker.Date.Value).TotalDays < 30)
                {
                    EndDatePicker.Date = StartDatePicker.Date.Value.AddDays(29);
                }
            }
            await LoadDashboardData();
            UpdateZoomButtonState();
        }

        private async void BtnZoomYear_Click(object sender, RoutedEventArgs e)
        {
            _currentGranularity = ChartGranularity.Year;
            // Extend date range ke minimal 1 tahun
            if (StartDatePicker.Date.HasValue && EndDatePicker.Date.HasValue)
            {
                if ((EndDatePicker.Date.Value - StartDatePicker.Date.Value).TotalDays < 365)
                {
                    EndDatePicker.Date = StartDatePicker.Date.Value.AddDays(364);
                }
            }
            await LoadDashboardData();
            UpdateZoomButtonState();
        }

        // --- VISUAL STATE MANAGEMENT ---

        private void UpdateZoomButtonState()
        {
            // Definisi warna untuk state aktif/inaktif
            var cyan = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xB4, 0xFF));
            var darkBg = new SolidColorBrush(Color.FromArgb(0xFF, 0x0D, 0x1B, 0x2A));
            var whiteFg = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));

            // Reset style semua tombol ke default
            this.BtnZoomDay.Background = darkBg;
            this.BtnZoomWeek.Background = darkBg;
            this.BtnZoomMonth.Background = darkBg;
            this.BtnZoomYear.Background = darkBg;

            this.BtnZoomDay.Foreground = whiteFg;
            this.BtnZoomWeek.Foreground = whiteFg;
            this.BtnZoomMonth.Foreground = whiteFg;
            this.BtnZoomYear.Foreground = whiteFg;

            // Highlight tombol yang sedang aktif
            switch (_currentGranularity)
            {
                case ChartGranularity.Day:
                    this.BtnZoomDay.Background = cyan;
                    this.TxtChartGranularity.Text = "📊 Daily View";
                    break;
                case ChartGranularity.Week:
                    this.BtnZoomWeek.Background = cyan;
                    this.TxtChartGranularity.Text = "📅 Weekly View";
                    break;
                case ChartGranularity.Month:
                    this.BtnZoomMonth.Background = cyan;
                    this.TxtChartGranularity.Text = "📆 Monthly View";
                    break;
                case ChartGranularity.Year:
                    this.BtnZoomYear.Background = cyan;
                    this.TxtChartGranularity.Text = "📊 Yearly View";
                    break;
            }
        }

        // --- ANIMATION & HOVER EFFECTS (GLASSMORPHISM 3D) ---

        private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border card)
            {
                // Efek mengangkat kartu (Translation Z)
                card.Translation = new System.Numerics.Vector3(0, -6, 20);
                // Ubah kursor jadi tangan
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            }
        }

        private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border card)
            {
                // Kembalikan posisi kartu
                card.Translation = new System.Numerics.Vector3(0, 0, 0);
                ProtectedCursor = null;
            }
        }

        // Wrapper method untuk event handler XAML agar lebih rapi
        private void KpiCard_PointerEntered(object sender, PointerRoutedEventArgs e) => Card_PointerEntered(sender, e);
        private void KpiCard_PointerExited(object sender, PointerRoutedEventArgs e) => Card_PointerExited(sender, e);
        private void FilterCard_PointerEntered(object sender, PointerRoutedEventArgs e) => Card_PointerEntered(sender, e);
        private void FilterCard_PointerExited(object sender, PointerRoutedEventArgs e) => Card_PointerExited(sender, e);
        private void ChartCard_PointerEntered(object sender, PointerRoutedEventArgs e) => Card_PointerEntered(sender, e);
        private void ChartCard_PointerExited(object sender, PointerRoutedEventArgs e) => Card_PointerExited(sender, e);
        private void AnalyticsCard_PointerEntered(object sender, PointerRoutedEventArgs e) => Card_PointerEntered(sender, e);
        private void AnalyticsCard_PointerExited(object sender, PointerRoutedEventArgs e) => Card_PointerExited(sender, e);


        // --- CORE LOGIC: DATA PROCESSING ---

        private async Task LoadDashboardData()
        {
            try
            {
                // 1. Ambil data full dari service
                _allData = await _service.GetAllTicketsAsync();
                if (_allData == null) _allData = new List<TicketLog>();

                // 2. Ambil parameter filter dari UI
                DateTime start = this.StartDatePicker.Date?.Date ?? DateTime.Now.AddDays(-6);
                DateTime end = this.EndDatePicker.Date?.Date ?? DateTime.Now;
                DateTime endInclusive = end.AddDays(1).AddTicks(-1); // Sampai akhir hari (23:59:59)

                string searchQuery = this.DashboardSearchBox.Text?.ToLower() ?? "";

                // 3. Lakukan Filtering di Memory
                var filteredData = _allData.Where(t =>
                {
                    // Filter Tanggal (Cek DispatchTime dulu, kalau null pake OccurTime)
                    bool dateMatch = false;
                    if (DateTime.TryParse(t.DispatchTime, out DateTime dt))
                        dateMatch = dt >= start && dt <= endInclusive;
                    else if (DateTime.TryParse(t.OccurTime, out DateTime ot))
                        dateMatch = ot >= start && ot <= endInclusive;

                    // Filter Search Text (Multi-column search)
                    bool searchMatch = string.IsNullOrEmpty(searchQuery) ||
                                       (t.TT_IOH?.ToLower().Contains(searchQuery) ?? false) ||
                                       (t.SegmentPM?.ToLower().Contains(searchQuery) ?? false) ||
                                       (t.RootCause?.ToLower().Contains(searchQuery) ?? false);

                    return dateMatch && searchMatch;
                }).ToList();

                // 4. Update Semua Komponen UI
                UpdateKpiCards(filteredData);
                UpdateQuickStats(filteredData, start, end);
                UpdateTrendChart(filteredData, start, end);
                UpdateRootCauseChart(filteredData);
                UpdateProblematicSegments(filteredData);
                UpdateRecentActivity(filteredData);
                UpdateZoomButtonState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.Message}");
                // Optional: Tampilkan dialog error jika perlu
            }
        }

        private void UpdateKpiCards(List<TicketLog> data)
        {
            int total = data.Count;
            // Tiket OPEN: Status tidak mengandung "UP" dan ClosedTime kosong
            int open = data.Count(x => (x.Status == null || !x.Status.ToUpper().Contains("UP")) && string.IsNullOrEmpty(x.ClosedTime));
            // Tiket SOLVED: ClosedTime ada isinya
            int solved = data.Count(x => !string.IsNullOrEmpty(x.ClosedTime));

            // Perhitungan MTTR dan SLA
            double totalHours = 0;
            int mttrCount = 0;
            int slaMet = 0;

            foreach (var t in data)
            {
                if (DateTime.TryParse(t.OccurTime, out DateTime start) && DateTime.TryParse(t.ClosedTime, out DateTime stop))
                {
                    var dur = stop - start;
                    totalHours += dur.TotalHours;
                    mttrCount++;

                    // Asumsi SLA standard = 4 Jam
                    if (dur.TotalHours <= 4) slaMet++;
                }
            }

            double avgMttr = mttrCount > 0 ? totalHours / mttrCount : 0;
            double slaPercent = mttrCount > 0 ? (double)slaMet / mttrCount * 100 : 100;

            // Binding data ke UI TextBlock
            this.TxtTotalIncidents.Text = total.ToString("N0");
            this.TxtOpenIssues.Text = open.ToString("N0");
            this.TxtSolved.Text = solved.ToString("N0");
            this.TxtAvgMttr.Text = $"{avgMttr:F1}h";
            this.TxtSlaCompliance.Text = $"{slaPercent:F0}%";

            // Update indikator trend (Logic sederhana)
            this.TxtTotalChange.Text = $"+{total} this period";
            this.TxtOpenChange.Text = open > 5 ? "⚠️ High Volume" : open > 0 ? "⚠️ Active" : "✅ Clear";
            this.TxtSolvedRate.Text = $"{(total > 0 ? (solved * 100.0 / total) : 0):F0}% resolved";
            this.TxtMttrTrend.Text = avgMttr < 2 ? "⚡ Excellent" : avgMttr < 4 ? "✅ Good" : "⚠️ Needs Attention";

            // Animasi progress bar SLA
            this.ProgressSla.Value = Math.Min(slaPercent, 100);
        }

        private void UpdateQuickStats(List<TicketLog> data, DateTime start, DateTime end)
        {
            // 1. Average Response Time (Dispatch - Occur)
            var responses = new List<double>();
            foreach (var t in data)
            {
                if (DateTime.TryParse(t.OccurTime, out DateTime occurTime) &&
                    DateTime.TryParse(t.DispatchTime, out DateTime dispatchTime))
                {
                    var diff = (dispatchTime - occurTime).TotalMinutes;
                    if (diff > 0) responses.Add(diff);
                }
            }
            double avgResponse = responses.Count > 0 ? responses.Average() : 0;
            this.TxtAvgResponse.Text = avgResponse > 0 ? $"{avgResponse:F0} min" : "N/A";

            // 2. This Month Incidents (Bandingkan dengan bulan sekarang)
            var thisMonth = data.Count(t =>
            {
                if (DateTime.TryParse(t.OccurTime, out DateTime dt))
                    return dt.Year == DateTime.Now.Year && dt.Month == DateTime.Now.Month;
                return false;
            });
            this.TxtThisMonth.Text = thisMonth.ToString();

            // 3. Peak Hour (Jam tersibuk)
            var hourGroups = new Dictionary<int, int>();
            foreach (var t in data)
            {
                if (DateTime.TryParse(t.OccurTime, out DateTime dt))
                {
                    int hour = dt.Hour;
                    if (!hourGroups.ContainsKey(hour)) hourGroups[hour] = 0;
                    hourGroups[hour]++;
                }
            }

            int peakHour = -1;
            if (hourGroups.Count > 0)
            {
                peakHour = hourGroups.OrderByDescending(x => x.Value).First().Key;
            }
            this.TxtPeakHour.Text = peakHour >= 0 ? $"{peakHour:D2}:00" : "-";

            // 4. Uptime Approximation (100% - %Incidents vs Total Assets)
            // Ini hitungan kasar berdasarkan rasio tiket vs waktu
            // Logic: Semakin banyak tiket resolved, uptime dianggap terjaga
            double uptime = 100.0;
            if (data.Count > 0)
            {
                // Simple logic: Tiap insiden mengurangi 0.1% uptime (contoh)
                uptime = 100.0 - (data.Count * 0.05);
                if (uptime < 90) uptime = 90.0; // Floor
            }
            this.TxtUptime.Text = $"{uptime:F2}%";
        }

        private void UpdateTrendChart(List<TicketLog> data, DateTime start, DateTime end)
        {
            List<DateTime> dates = new List<DateTime>();
            List<int> values = new List<int>();
            List<string> labels = new List<string>();

            // Generate sumbu X (Tanggal) berdasarkan Granularity
            switch (_currentGranularity)
            {
                case ChartGranularity.Day:
                    for (var dt = start; dt <= end; dt = dt.AddDays(1)) dates.Add(dt);
                    break;

                case ChartGranularity.Week:
                    var weekStart = start;
                    while (weekStart <= end)
                    {
                        dates.Add(weekStart);
                        weekStart = weekStart.AddDays(7);
                    }
                    break;

                case ChartGranularity.Month:
                    var monthStart = new DateTime(start.Year, start.Month, 1);
                    while (monthStart <= end)
                    {
                        dates.Add(monthStart);
                        monthStart = monthStart.AddMonths(1);
                    }
                    break;

                case ChartGranularity.Year:
                    var yearStart = new DateTime(start.Year, 1, 1);
                    while (yearStart <= end)
                    {
                        dates.Add(yearStart);
                        yearStart = yearStart.AddYears(1);
                    }
                    break;
            }

            // Hitung jumlah tiket per periode
            foreach (var date in dates)
            {
                int count = 0;

                switch (_currentGranularity)
                {
                    case ChartGranularity.Day:
                        count = data.Count(t => DateTime.TryParse(t.DispatchTime, out DateTime dt) && dt.Date == date.Date);
                        labels.Add(date.ToString("dd MMM"));
                        break;

                    case ChartGranularity.Week:
                        var weekEnd = date.AddDays(6);
                        count = data.Count(t => DateTime.TryParse(t.DispatchTime, out DateTime dt) && dt.Date >= date.Date && dt.Date <= weekEnd.Date);
                        labels.Add($"W{GetWeekNumber(date)}");
                        break;

                    case ChartGranularity.Month:
                        count = data.Count(t => DateTime.TryParse(t.DispatchTime, out DateTime dt) && dt.Year == date.Year && dt.Month == date.Month);
                        labels.Add(date.ToString("MMM yy"));
                        break;

                    case ChartGranularity.Year:
                        count = data.Count(t => DateTime.TryParse(t.DispatchTime, out DateTime dt) && dt.Year == date.Year);
                        labels.Add(date.ToString("yyyy"));
                        break;
                }
                values.Add(count);
            }

            // Setup Chart Series (Line Chart dengan Gradient)
            this.DashboardTrendChart.Series = new ISeries[]
            {
                new LineSeries<int>
                {
                    Values = values,
                    Stroke = new SolidColorPaint(SKColor.Parse("#3B82F6")) { StrokeThickness = 3 },
                    Fill = new LinearGradientPaint(
                        new [] { SKColor.Parse("#803B82F6"), SKColor.Parse("#003B82F6") },
                        new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                    GeometrySize = 10,
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#3B82F6")) { StrokeThickness = 2 },
                    GeometryFill = new SolidColorPaint(SKColor.Parse("#0F172A")),
                    LineSmoothness = 0.5,
                    Name = "Incidents"
                }
            };

            // Setup Axis X
            this.DashboardTrendChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                    TextSize = 12
                }
            };

            // Hide Axis Y (biar bersih)
            this.DashboardTrendChart.YAxes = new Axis[] { new Axis { IsVisible = false } };
        }

        private void UpdateRootCauseChart(List<TicketLog> data)
        {
            // Group by RootCause, ambil Top 5
            var grouped = data
                .GroupBy(x => string.IsNullOrWhiteSpace(x.RootCause) ? "Unknown" : x.RootCause)
                .Select(g => new { Cause = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var series = new List<ISeries>();
            // Palet warna modern
            var colors = new[] { "#F472B6", "#A78BFA", "#34D399", "#60A5FA", "#FBBF24" };
            int i = 0;

            foreach (var item in grouped)
            {
                series.Add(new PieSeries<int>
                {
                    Values = new[] { item.Count },
                    Name = item.Cause,
                    InnerRadius = 60, // Efek Donut
                    Fill = new SolidColorPaint(SKColor.Parse(colors[i % colors.Length])),
                    Pushout = 2,
                    HoverPushout = 10,
                });
                i++;
            }

            this.RootCauseChart.Series = series;
        }

        private void UpdateProblematicSegments(List<TicketLog> data)
        {
            // Cari segmen/daerah yang paling sering bermasalah
            var segments = data
                .Where(x => !string.IsNullOrEmpty(x.SegmentPM))
                .GroupBy(x => x.SegmentPM)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(8) // Top 8
                .ToList();

            // Normalisasi nilai untuk progress bar (Persentase relatif terhadap item terbesar)
            var displayList = new List<object>();
            if (segments.Count > 0)
            {
                int maxVal = segments.Max(s => s.Count);
                foreach (var s in segments)
                {
                    displayList.Add(new
                    {
                        Name = s.Name,
                        Count = (int)((double)s.Count / maxVal * 100), // Persentase untuk bar length
                        RealCount = s.Count // Nilai asli untuk ditampilkan di teks
                    });
                }
            }

            this.ListTopSegments.ItemsSource = displayList;
        }

        private void UpdateRecentActivity(List<TicketLog> data)
        {
            // Ambil 15 aktivitas terakhir berdasarkan waktu kejadian
            var activity = data
                .OrderByDescending(x => ParseDateSafe(x.OccurTime))
                .Take(15)
                .Select(x => new
                {
                    Timestamp = x.OccurTime,
                    Message = $"{x.TT_IOH} - {x.SegmentPM}",
                    Status = x.Status ?? "Unknown"
                })
                .ToList();

            this.ListRecentActivity.ItemsSource = activity;
        }

        // --- HELPER METHODS ---

        private int GetWeekNumber(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        private DateTime ParseDateSafe(string dateStr)
        {
            if (DateTime.TryParse(dateStr, out DateTime dt)) return dt;
            return DateTime.MinValue;
        }
    }
}