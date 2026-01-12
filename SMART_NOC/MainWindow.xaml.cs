using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SMART_NOC.Models;
using SMART_NOC.Services;
using SMART_NOC.Views;
using System;
using WinUIEx;
using Microsoft.UI;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;

namespace SMART_NOC
{
    public sealed partial class MainWindow : WindowEx
    {
        public static MainWindow? Instance { get; private set; }

        // 🔥 PROPERTI BARU UNTUK PROGRESS BAR 🔥
        public bool IsPaused { get; set; } = false;
        public event Action? OnCancelRequested;
        
        // 🔥 NEW: Reference ke LiveMapPage untuk refresh data 🔥
        public LiveMapPage? MapPageInstance { get; set; }

        public MainWindow()
        {
            this.InitializeComponent();
            Instance = this;

            this.Title = "SMART NOC COMMANDER";
            this.PersistenceId = "SmartNocMainWindow";
            this.MinWidth = 1000;
            this.MinHeight = 700;

            this.SystemBackdrop = new MicaBackdrop();
            CustomizeTitleBar();

            var titleBar = this.AppWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            this.SetTitleBar(WindowDragRegion);

            // 🔥 INITIALIZE ACTIVITY MONITOR 🔥
            ActivityMonitor monitor = ActivityMonitor.Instance;
            monitor.RegisterWindow(this);
            monitor.OnAnomalyDetected += OnActivityAnomalyDetected;
            
            AddLog("System Initialized. Ready for command.");
            AddLog("🎯 Activity monitoring enabled - all interactions tracked");

            // 🔥 PROPER TITLEBAR DRAG TRACKING (FIXED V3 - CRITICAL FIXES) 🔥
            Point titleBarDragStartPos = new Point(0, 0);
            DateTime titleBarDragStartTime = DateTime.Now;
            bool isTitleBarDragging = false;

            WindowDragRegion.PointerPressed += (sender, e) =>
            {
                try
                {
                    titleBarDragStartPos = e.GetCurrentPoint(null).Position;
                    titleBarDragStartTime = DateTime.Now;
                    isTitleBarDragging = true;
                    AddLog($"📍 TITLEBAR DRAG START [X: {titleBarDragStartPos.X:F0}, Y: {titleBarDragStartPos.Y:F0}]");
                }
                catch { }
            };

            WindowDragRegion.PointerMoved += (sender, e) =>
            {
                try
                {
                    if (!isTitleBarDragging) return;
                    
                    Point currentPos = e.GetCurrentPoint(null).Position;
                    
                    // 🔥 FIX #1: Calculate distance from preserved START (never reset) 🔥
                    double distanceFromStart = CalculateDistance(titleBarDragStartPos, currentPos);
                    
                    // 🔥 FIX #2: Lower threshold from 2px to 0.5px for accurate logging 🔥
                    if (distanceFromStart > 0.5)  // ← LOWERED from 2px to 0.5px
                    {
                        double duration = (DateTime.Now - titleBarDragStartTime).TotalMilliseconds;
                        double speedFromStart = distanceFromStart / (duration > 0 ? duration : 1);
                        
                        AddLog($"📍 TITLEBAR DRAGGING [Current: {currentPos.X:F0}, {currentPos.Y:F0}] " +
                               $"FROM_START: {distanceFromStart:F1}px Speed: {speedFromStart:F2}px/ms");
                    }
                    
                    // 🔥 FIX #3: Removed titleBarLastPos (was causing confusion) 🔥
                    // NO UPDATE needed - just calculate from preserved start
                }
                catch { }
            };

            WindowDragRegion.PointerReleased += (sender, e) =>
            {
                try
                {
                    if (!isTitleBarDragging) return;
                    
                    isTitleBarDragging = false;
                    Point endPos = e.GetCurrentPoint(null).Position;
                    
                    // 🔥 GUARANTEED: Use preserved START position (never changed) 🔥
                    double finalDistance = CalculateDistance(titleBarDragStartPos, endPos);
                    double finalDuration = (DateTime.Now - titleBarDragStartTime).TotalMilliseconds;
                    
                    AddLog($"📍 TITLEBAR DRAG END [Final Distance: {finalDistance:F1}px, Duration: {finalDuration:F0}ms] " +
                           $"[{titleBarDragStartPos.X:F0},{titleBarDragStartPos.Y:F0}] → [{endPos.X:F0},{endPos.Y:F0}]");
                    
                    ActivityMonitor.Instance.LogTitleBarDragAttempt(
                        "window_drag",
                        titleBarDragStartPos,  // ✅ Preserved original start position
                        endPos,
                        finalDuration
                    );
                }
                catch { }
            };

            // 🔥 LOGIC: CEK CRASH REPORT SETELAH STARTUP 🔥
            DispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(1500);
                await CheckForCrashReport();
            });
        }

        // 🔥 HELPER: Calculate distance between two points 🔥
        private double CalculateDistance(Point p1, Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // 🔥 HANDLE ANOMALY DETECTION 🔥
        private void OnActivityAnomalyDetected(string anomalyMessage)
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                AddLog(anomalyMessage);
                CrashLogger.Log(new Exception(anomalyMessage), "ActivityAnomaly", "WARNING");
            });
        }

        // =========================================================
        // 🔥 1. LOGIC PROGRESS BAR & CANCEL (FITUR BARU) 🔥
        // =========================================================

        public void ShowProgress(bool isVisible, string processName = "PROCESSING...")
        {
            if (DispatcherQueue == null) return;
            DispatcherQueue.TryEnqueue(() =>
            {
                if (ProcessPanel != null)
                {
                    ProcessPanel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                    if (isVisible)
                    {
                        TxtProcessName.Text = processName;
                        MainProgressBar.Value = 0;
                        TxtProcessPercent.Text = "0%";
                        // Sembunyikan status log biasa biar rapi
                        if (StatusLog != null) StatusLog.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        // Munculkan lagi status log biasa
                        if (StatusLog != null) StatusLog.Visibility = Visibility.Visible;
                    }
                }
            });
        }

        public void UpdateProgress(double value, int currentItem, int totalItems)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (MainProgressBar != null) MainProgressBar.Value = value;
                if (TxtProcessPercent != null) TxtProcessPercent.Text = $"{Math.Floor(value)}%";

                // Opsional: Tampilkan detail angka di StatusLog jika perlu
                // if (StatusLog != null) StatusLog.Text = $"Processing: {currentItem}/{totalItems}";
            });
        }

        private void BtnCancelProcess_Click(object sender, RoutedEventArgs e)
        {
            // Panggil event agar Page (HistoryPage) tahu user minta stop
            OnCancelRequested?.Invoke();
            AddLog("⚠️ USER REQUESTED TO KILL PROCESS.");
        }

        // =========================================================
        // 🔥 2. LOGIC DETEKSI FORCE CLOSE (POPUP) 🔥
        // =========================================================
        private async Task CheckForCrashReport()
        {
            try
            {
                string folder = ApplicationData.Current.LocalFolder.Path;
                string lastCrashPath = Path.Combine(folder, "last_crash.txt");

                if (File.Exists(lastCrashPath))
                {
                    string errorContent = await File.ReadAllTextAsync(lastCrashPath);

                    if (this.Content is FrameworkElement fe && fe.XamlRoot != null)
                    {
                        ContentDialog crashDialog = new ContentDialog
                        {
                            Title = "⚠️ FORCE CLOSE TERDETEKSI",
                            Content = new ScrollViewer
                            {
                                Content = new TextBlock
                                {
                                    Text = "Aplikasi terhenti tiba-tiba pada sesi sebelumnya. Berikut detail errornya:\n\n" + errorContent,
                                    TextWrapping = TextWrapping.Wrap,
                                    FontFamily = new FontFamily("Consolas"),
                                    FontSize = 12
                                },
                                Height = 300,
                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                            },
                            CloseButtonText = "OK, Mengerti",
                            XamlRoot = fe.XamlRoot,
                            DefaultButton = ContentDialogButton.Close
                        };

                        await crashDialog.ShowAsync();
                    }

                    File.Delete(lastCrashPath);
                }
            }
            catch { /* Silent fail */ }
        }

        // =========================================================
        // 3. TITLE BAR & NAVIGATION (STANDARD)
        // =========================================================

        private void CustomizeTitleBar()
        {
            var titleBar = this.AppWindow.TitleBar;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(100, 255, 255, 255);
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(30, 255, 255, 255);
            titleBar.ButtonPressedForegroundColor = Colors.White;
            titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(50, 255, 255, 255);
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(DashboardPage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                string tag = item.Tag?.ToString() ?? "";
                Type? pageType = tag switch
                {
                    "Dashboard" => typeof(DashboardPage),
                    "CreateTicket" => typeof(TicketPage),
                    "LiveMap" => typeof(LiveMapPage),
                    "History" => typeof(HistoryPage),
                    "HandoverLog" => typeof(HandoverPage),
                    "Settings" => typeof(SettingsPage),
                    _ => null
                };

                if (pageType != null)
                {
                    ContentFrame.Navigate(pageType, null, new Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionInfo() { Effect = Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionEffect.FromRight });
                }
            }
        }

        // =========================================================
        // 4. LOGGING & DEV TOOLS
        // =========================================================

        public void AddLog(string message)
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                try
                {
                    string time = DateTime.Now.ToString("HH:mm:ss.fff");
                    string fullLog = $"[{time}] {message}";
                    
                    if (StatusLog != null) 
                        StatusLog.Text = fullLog;

                    if (DevLogBox != null)
                    {
                        DevLogBox.Text += fullLog + Environment.NewLine;
                        
                        // 🔥 TRY AUTO-SCROLL TO BOTTOM (safe version) 🔥
                        try
                        {
                            // Find parent ScrollViewer
                            var parent = VisualTreeHelper.GetParent(DevLogBox);
                            while (parent != null)
                            {
                                if (parent is ScrollViewer sv)
                                {
                                    sv.ChangeView(0, sv.ScrollableHeight, 1);
                                    break;
                                }
                                parent = VisualTreeHelper.GetParent(parent);
                            }
                        }
                        catch { /* Safe to ignore scroll errors */ }
                    }

                    // 🔥 ALSO LOG TO CRASH LOGGER 🔥
                    try
                    {
                        if (!message.StartsWith("System"))
                        {
                            CrashLogger.LogInfo(message, "MainWindow");
                        }
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AddLog error: {ex.Message}");
                }
            });
        }

        private void AiToggle_Toggled(object sender, RoutedEventArgs e)
        {
            AddLog($"AI Status: {((ToggleSwitch)sender).IsOn}");
        }

        private void DevToggle_Toggled(object sender, RoutedEventArgs e)
        {
            bool isDev = ((ToggleSwitch)sender).IsOn;
            if (DevPanel != null) DevPanel.Visibility = isDev ? Visibility.Visible : Visibility.Collapsed;
            AddLog($"Developer Mode: {isDev}");
            
            // 🔥 SHOW ACTIVITY STATS WHEN DEV MODE ENABLED 🔥
            if (isDev)
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    string stats = ActivityMonitor.Instance.GetDetailedReport();
                    foreach (var line in stats.Split('\n'))
                    {
                        AddLog(line);
                    }
                });
            }
        }

        private void BtnCopyLog_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(DevLogBox.Text))
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(DevLogBox.Text);
                Clipboard.SetContent(dataPackage);
                AddLog("All logs copied to clipboard.");
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            DevLogBox.Text = "";
            AddLog("Logs cleared.");
        }

        private void BtnOpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = CrashLogger.GetLogFilePath();
                if (System.IO.File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                else
                {
                    AddLog("Log file not found yet.");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Failed to open log file: {ex.Message}");
            }
        }
    }
}