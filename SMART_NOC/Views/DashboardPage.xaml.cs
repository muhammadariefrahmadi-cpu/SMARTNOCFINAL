using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using SMART_NOC.Services;
using SMART_NOC.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using Windows.UI;

namespace SMART_NOC.Views
{
    public sealed partial class DashboardPage : Page
    {
        private HistoryService _service = new HistoryService();
        private List<TicketLog> _allData = new List<TicketLog>();
        
        // Enum untuk tracking zoom level
        private enum ChartGranularity { Day, Week, Month, Year }
        private ChartGranularity _currentGranularity = ChartGranularity.Day;

        public SolidColorPaint LegendTextPaint { get; set; } = new SolidColorPaint
        {
            Color = SKColors.WhiteSmoke,
            SKTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
        };

        public DashboardPage()
        {
            this.InitializeComponent();

            StartDatePicker.Date = DateTimeOffset.Now.AddDays(-6);
            EndDatePicker.Date = DateTimeOffset.Now;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadDashboardData();
        }

        private void Filter_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) 
        { 
            _currentGranularity = ChartGranularity.Day; // Reset zoom saat filter berubah
            LoadDashboardData(); 
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) 
        { 
            _currentGranularity = ChartGranularity.Day;
            LoadDashboardData(); 
        }

        // Event handlers untuk zoom buttons
        private void BtnZoomDay_Click(object sender, RoutedEventArgs e)
        {
            _currentGranularity = ChartGranularity.Day;
            LoadDashboardData();
            UpdateZoomButtonState();
        }

        private void BtnZoomWeek_Click(object sender, RoutedEventArgs e)
        {
            _currentGranularity = ChartGranularity.Week;
            // Extend date range jika perlu
            if ((EndDatePicker.Date.Value - StartDatePicker.Date.Value).TotalDays < 7)
            {
                EndDatePicker.Date = StartDatePicker.Date.Value.AddDays(6);
            }
            LoadDashboardData();
            UpdateZoomButtonState();
        }

        private void BtnZoomMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentGranularity = ChartGranularity.Month;
            // Extend date range ke 1 bulan
            if ((EndDatePicker.Date.Value - StartDatePicker.Date.Value).TotalDays < 30)
            {
                EndDatePicker.Date = StartDatePicker.Date.Value.AddDays(29);
            }
            LoadDashboardData();
            UpdateZoomButtonState();
        }

        private void BtnZoomYear_Click(object sender, RoutedEventArgs e)
        {
            _currentGranularity = ChartGranularity.Year;
            // Extend date range ke 1 tahun
            if ((EndDatePicker.Date.Value - StartDatePicker.Date.Value).TotalDays < 365)
            {
                EndDatePicker.Date = StartDatePicker.Date.Value.AddDays(364);
            }
            LoadDashboardData();
            UpdateZoomButtonState();
        }

        private void UpdateZoomButtonState()
        {
            var cyan = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xB4, 0xFF));
            var darkBg = new SolidColorBrush(Color.FromArgb(0xFF, 0x0D, 0x1B, 0x2A));
            var whiteFg = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            var darkFg = new SolidColorBrush(Color.FromArgb(0xFF, 0x94, 0xA3, 0xB8));

            // Reset semua buttons
            this.BtnZoomDay.Background = darkBg;
            this.BtnZoomWeek.Background = darkBg;
            this.BtnZoomMonth.Background = darkBg;
            this.BtnZoomYear.Background = darkBg;

            // Reset foreground
            this.BtnZoomDay.Foreground = whiteFg;
            this.BtnZoomWeek.Foreground = whiteFg;
            this.BtnZoomMonth.Foreground = whiteFg;
            this.BtnZoomYear.Foreground = whiteFg;

            // Highlight button yang aktif
            switch (_currentGranularity)
            {
                case ChartGranularity.Day:
                    this.BtnZoomDay.Background = cyan;
                    this.BtnZoomDay.Foreground = whiteFg;
                    this.TxtChartGranularity.Text = "📊 Daily View";
                    break;
                case ChartGranularity.Week:
                    this.BtnZoomWeek.Background = cyan;
                    this.BtnZoomWeek.Foreground = whiteFg;
                    this.TxtChartGranularity.Text = "📅 Weekly View";
                    break;
                case ChartGranularity.Month:
                    this.BtnZoomMonth.Background = cyan;
                    this.BtnZoomMonth.Foreground = whiteFg;
                    this.TxtChartGranularity.Text = "📆 Monthly View";
                    break;
                case ChartGranularity.Year:
                    this.BtnZoomYear.Background = cyan;
                    this.BtnZoomYear.Foreground = whiteFg;
                    this.TxtChartGranularity.Text = "📊 Yearly View";
                    break;
            }
        }

        // 🔥 NEW: HOVER EFFECTS 🔥
        private void KpiCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border card)
                card.Translation = new System.Numerics.Vector3(0, -6, 20);
        }

        private void KpiCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border card)
                card.Translation = new System.Numerics.Vector3(0, 0, 0);
        }

        private void FilterCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border card)
                card.Translation = new System.Numerics.Vector3(0, -4, 16);
        }

        private void FilterCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border card)
                card.Translation = new System.Numerics.Vector3(0, 0, 0);
        }

        private void ChartCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border card)
                card.Translation = new System.Numerics.Vector3(0, -6, 20);
        }

        private void ChartCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border card)
                card.Translation = new System.Numerics.Vector3(0, 0, 0);
        }

        private void AnalyticsCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border card)
                card.Translation = new System.Numerics.Vector3(0, -6, 20);
        }

        private void AnalyticsCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border card)
                card.Translation = new System.Numerics.Vector3(0, 0, 0);
        }

        private async void LoadDashboardData()
        {
            try
            {
                _allData = await _service.GetAllTicketsAsync();
                if (_allData == null) _allData = new List<TicketLog>();

                DateTime start = this.StartDatePicker.Date?.Date ?? DateTime.Now.AddDays(-6);
                DateTime end = this.EndDatePicker.Date?.Date ?? DateTime.Now;
                DateTime endInclusive = end.AddDays(1).AddTicks(-1);

                string searchQuery = this.DashboardSearchBox.Text.ToLower();

                var filteredData = _allData.Where(t =>
                {
                    bool dateMatch = false;
                    if (DateTime.TryParse(t.DispatchTime, out DateTime dt)) dateMatch = dt >= start && dt <= endInclusive;
                    else if (DateTime.TryParse(t.OccurTime, out DateTime ot)) dateMatch = ot >= start && ot <= endInclusive;

                    bool searchMatch = string.IsNullOrEmpty(searchQuery) ||
                                       (t.TT_IOH?.ToLower().Contains(searchQuery) ?? false) ||
                                       (t.SegmentPM?.ToLower().Contains(searchQuery) ?? false) ||
                                       (t.RootCause?.ToLower().Contains(searchQuery) ?? false);

                    return dateMatch && searchMatch;
                }).ToList();

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
            }
        }

        private void UpdateKpiCards(List<TicketLog> data)
        {
            int total = data.Count;
            int open = data.Count(x => !x.Status.Contains("UP") && string.IsNullOrEmpty(x.ClosedTime));
            int solved = data.Count(x => !string.IsNullOrEmpty(x.ClosedTime));

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
                    if (dur.TotalHours <= 4) slaMet++;
                }
            }
            
            double avgMttr = mttrCount > 0 ? totalHours / mttrCount : 0;
            double slaPercent = mttrCount > 0 ? (double)slaMet / mttrCount * 100 : 100;

            this.TxtTotalIncidents.Text = total.ToString("N0");
            this.TxtOpenIssues.Text = open.ToString("N0");
            this.TxtSolved.Text = solved.ToString("N0");
            this.TxtAvgMttr.Text = $"{avgMttr:F1}h";
            this.TxtSlaCompliance.Text = $"{slaPercent:F0}%";

            // Update trend indicators
            this.TxtTotalChange.Text = $"+{total} this period";
            this.TxtOpenChange.Text = open > 5 ? "⚠️ High level" : open > 0 ? "⚠️ Active" : "✅ Clear";
            this.TxtSolvedRate.Text = $"{(total > 0 ? (solved * 100 / total) : 0):F0}% resolved";
            this.TxtMttrTrend.Text = avgMttr < 2 ? "↓ Excellent" : avgMttr < 4 ? "↓ Good" : "⚠️ Slow";
            
            this.ProgressSla.Value = Math.Min(slaPercent, 100);
        }

        private void UpdateQuickStats(List<TicketLog> data, DateTime start, DateTime end)
        {
            // Average response time
            var responses = new List<double>();
            foreach (var t in data)
            {
                if (DateTime.TryParse(t.OccurTime, out DateTime occurTime) && 
                    DateTime.TryParse(t.DispatchTime, out DateTime dispatchTime))
                {
                    responses.Add((dispatchTime - occurTime).TotalMinutes);
                }
            }
            double avgResponse = responses.Count > 0 ? responses.Average() : 0;
            this.TxtAvgResponse.Text = avgResponse > 0 ? $"{avgResponse:F0} min" : "N/A";

            // This month incidents
            var thisMonth = data.Where(t =>
            {
                if (DateTime.TryParse(t.OccurTime, out DateTime dt))
                    return dt.Year == DateTime.Now.Year && dt.Month == DateTime.Now.Month;
                return false;
            }).Count();
            this.TxtThisMonth.Text = thisMonth.ToString();

            // Peak hour (hour with most incidents)
            var hourGroups = new Dictionary<int, int>();
            foreach (var t in data)
            {
                if (DateTime.TryParse(t.OccurTime, out DateTime dt))
                {
                    int hour = dt.Hour;
                    if (hourGroups.ContainsKey(hour))
                        hourGroups[hour]++;
                    else
                        hourGroups[hour] = 1;
                }
            }
            int peakHour = hourGroups.Count > 0 ? hourGroups.OrderByDescending(x => x.Value).First().Key : -1;
            this.TxtPeakHour.Text = peakHour >= 0 ? $"{peakHour:D2}:00" : "N/A";

            // Calculate uptime percentage
            int totalTickets = data.Count;
            int resolvedTickets = data.Count(x => !string.IsNullOrEmpty(x.ClosedTime));
            double uptime = totalTickets > 0 ? (100 - (double)resolvedTickets / totalTickets * 100) : 100;
            this.TxtUptime.Text = $"{uptime:F1}%";
        }

        private void UpdateTrendChart(List<TicketLog> data, DateTime start, DateTime end)
        {
            List<DateTime> dates = new List<DateTime>();
            List<int> values = new List<int>();
            List<string> labels = new List<string>();

            // Generate dates berdasarkan granularity
            switch (_currentGranularity)
            {
                case ChartGranularity.Day:
                    for (var dt = start; dt <= end; dt = dt.AddDays(1))
                    {
                        dates.Add(dt);
                    }
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

            // Calculate values untuk setiap date period
            foreach (var date in dates)
            {
                int count = 0;

                switch (_currentGranularity)
                {
                    case ChartGranularity.Day:
                        count = data.Count(t =>
                        {
                            if (DateTime.TryParse(t.DispatchTime, out DateTime dt)) 
                                return dt.Date == date.Date;
                            return false;
                        });
                        labels.Add(date.ToString("dd MMM"));
                        break;

                    case ChartGranularity.Week:
                        var weekEnd = date.AddDays(6);
                        count = data.Count(t =>
                        {
                            if (DateTime.TryParse(t.DispatchTime, out DateTime dt))
                                return dt.Date >= date.Date && dt.Date <= weekEnd.Date;
                            return false;
                        });
                        labels.Add($"W{date:yyyyMMdd}");
                        break;

                    case ChartGranularity.Month:
                        count = data.Count(t =>
                        {
                            if (DateTime.TryParse(t.DispatchTime, out DateTime dt))
                                return dt.Year == date.Year && dt.Month == date.Month;
                            return false;
                        });
                        labels.Add(date.ToString("MMM yyyy"));
                        break;

                    case ChartGranularity.Year:
                        count = data.Count(t =>
                        {
                            if (DateTime.TryParse(t.DispatchTime, out DateTime dt))
                                return dt.Year == date.Year;
                            return false;
                        });
                        labels.Add(date.ToString("yyyy"));
                        break;
                }

                values.Add(count);
            }

            this.DashboardTrendChart.Series = new ISeries[]
            {
                new LineSeries<int>
                {
                    Values = values,
                    Stroke = new SolidColorPaint(SKColor.Parse("#3B82F6")) { StrokeThickness = 3 },
                    Fill = new LinearGradientPaint(
                        new [] { SKColor.Parse("#803B82F6"), SKColor.Parse("#003B82F6") },
                        new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                    GeometrySize = 12,
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#3B82F6")) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(SKColor.Parse("#0F172A")),
                    LineSmoothness = 0.5
                }
            };

            this.DashboardTrendChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                    TextSize = 12
                }
            };

            this.DashboardTrendChart.YAxes = new Axis[] { new Axis { IsVisible = false } };
        }

        private void UpdateRootCauseChart(List<TicketLog> data)
        {
            var grouped = data
                .GroupBy(x => string.IsNullOrWhiteSpace(x.RootCause) ? "Unknown" : x.RootCause)
                .Select(g => new { Cause = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var series = new List<ISeries>();
            var colors = new[] { "#F472B6", "#A78BFA", "#34D399", "#60A5FA", "#FBBF24" };
            int i = 0;

            foreach (var item in grouped)
            {
                series.Add(new PieSeries<int>
                {
                    Values = new[] { item.Count },
                    Name = item.Cause,
                    InnerRadius = 60,
                    Fill = new SolidColorPaint(SKColor.Parse(colors[i % colors.Length])),
                    Pushout = 2,
                    HoverPushout = 15,
                });
                i++;
            }

            this.RootCauseChart.Series = series;
        }

        private void UpdateProblematicSegments(List<TicketLog> data)
        {
            var segments = data
                .GroupBy(x => x.SegmentPM)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            // Normalize counts untuk progress bar
            if (segments.Count > 0)
            {
                var maxCount = segments.First().Count;
                segments = segments.Select(s => new { s.Name, Count = (int)((double)s.Count / maxCount * 100) }).ToList();
            }

            this.ListTopSegments.ItemsSource = segments;
        }

        private void UpdateRecentActivity(List<TicketLog> data)
        {
            var activity = data
                .OrderByDescending(x => x.OccurTime)
                .Take(10)
                .Select(x => new
                {
                    Timestamp = x.OccurTime,
                    Message = $"{x.TT_IOH} - {x.SegmentPM}",
                    Status = x.Status
                })
                .ToList();
            this.ListRecentActivity.ItemsSource = activity;
        }
    }
}