using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SMART_NOC.Services;
using SMART_NOC.Views;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SMART_NOC
{
    public partial class App : Application
    {
        // =========================================================
        // 🔥 GLOBAL PROPERTIES
        // =========================================================
        public static Window? MainWindow { get; set; }
        public static NocDatabase GlobalDatabase { get; private set; } = new NocDatabase();
        private Window? _splashWindow;
        private bool _crashHandlerAttached = false;

        public App()
        {
            this.InitializeComponent();

            // 🔥 ATTACH CRASH HANDLERS IMMEDIATELY 🔥
            try
            {
                AttachGlobalExceptionHandlers();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Failed to attach exception handlers: {ex.Message}");
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // 🔥 CHECK FOR PREVIOUS CRASH 🔥
            Task.Run(async () =>
            {
                await CheckAndShowPreviousCrash();
            });

            // STARTUP VIA SPLASH WINDOW
            _splashWindow = new SplashWindow();
            _splashWindow.Activate();
        }

        // =========================================================
        // 🔥 1. ATTACH ALL EXCEPTION HANDLERS 🔥
        // =========================================================
        private void AttachGlobalExceptionHandlers()
        {
            if (_crashHandlerAttached) return;

            try
            {
                // 🔥 XAML/UI EXCEPTIONS 🔥
                this.UnhandledException += App_UnhandledException;

                // 🔥 BACKGROUND TASK EXCEPTIONS 🔥
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

                // 🔥 SYSTEM-LEVEL EXCEPTIONS 🔥
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                _crashHandlerAttached = true;
                CrashLogger.LogInfo("All exception handlers attached successfully", "Startup");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception handler attachment failed: {ex.Message}");
            }
        }

        // =========================================================
        // 🔥 2. CRASH DETECTION & REPORTING
        // =========================================================

        private async Task CheckAndShowPreviousCrash()
        {
            try
            {
                // 🔥 CHECK MULTIPLE CRASH REPORT LOCATIONS 🔥
                string crashReport = "";
                string crashReportPath = "";
                
                // Try primary location first
                crashReport = CrashLogger.ReadCrashReport();
                crashReportPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SMART_NOC",
                    "last_crash.txt"
                );
                
                // If not found, try emergency crash log
                if (string.IsNullOrEmpty(crashReport))
                {
                    string emergencyPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "SMART_NOC",
                        "emergency_crash.log"
                    );
                    
                    if (File.Exists(emergencyPath))
                    {
                        try
                        {
                            crashReport = File.ReadAllText(emergencyPath);
                            crashReportPath = emergencyPath;
                            CrashLogger.LogInfo("Loaded crash report from emergency location", "CrashDetection");
                        }
                        catch { }
                    }
                }
                
                // If still not found, try main crash log (last entry)
                if (string.IsNullOrEmpty(crashReport))
                {
                    string logPath = CrashLogger.GetLogFilePath();
                    if (File.Exists(logPath))
                    {
                        try
                        {
                            var lines = File.ReadAllLines(logPath);
                            // Find last CRASH entry
                            for (int i = lines.Length - 1; i >= 0; i--)
                            {
                                if (lines[i].Contains("CRASH") || lines[i].Contains("CRITICAL"))
                                {
                                    // Get context around this line
                                    int start = Math.Max(0, i - 20);
                                    int count = Math.Min(lines.Length - start, 30);
                                    crashReport = string.Join("\n", lines, start, count);
                                    CrashLogger.LogInfo("Loaded crash report from main log", "CrashDetection");
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                }
                
                if (!string.IsNullOrEmpty(crashReport))
                {
                    CrashLogger.LogInfo("Previous crash detected on startup", "CrashDetection");

                    // 🔥 SHOW DIALOG AFTER APP FULLY LOADED 🔥
                    await Task.Delay(2000); // Wait for UI to be ready

                    if (MainWindow != null && MainWindow.Content is FrameworkElement fe && fe.XamlRoot != null)
                    {
                        ContentDialog crashDialog = new ContentDialog
                        {
                            Title = "⚠️ PREVIOUS SESSION CRASHED",
                            Content = new ScrollViewer
                            {
                                Content = new TextBlock
                                {
                                    Text = crashReport,
                                    TextWrapping = TextWrapping.Wrap,
                                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                                    FontSize = 11,
                                    IsTextSelectionEnabled = true
                                },
                                Height = 400,
                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                            },
                            PrimaryButtonText = "View Full Logs",
                            SecondaryButtonText = "Clear Logs",
                            CloseButtonText = "OK",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = fe.XamlRoot
                        };

                        try
                        {
                            var result = await crashDialog.ShowAsync();

                            // Safe access to MainWindow
                            var mw = MainWindow as SMART_NOC.MainWindow;
                            if (result == ContentDialogResult.Primary && mw != null)
                            {
                                mw.AddLog("─── FULL CRASH LOG ───");
                                mw.AddLog(CrashLogger.ReadLog());
                                mw.AddLog(CrashLogger.GetSessionStats());
                            }
                            else if (result == ContentDialogResult.Secondary)
                            {
                                CrashLogger.ClearLog();
                                if (mw != null) mw.AddLog("✅ Logs cleared");
                            }
                        }
                        catch (Exception dialogEx)
                        {
                            Debug.WriteLine($"⚠️ Crash dialog error: {dialogEx.Message}");
                        }
                    }

                    // 🔥 DELETE CRASH REPORT FILES AFTER SHOWING 🔥
                    try
                    {
                        // Delete primary location
                        if (File.Exists(crashReportPath))
                            File.Delete(crashReportPath);
                        
                        // Also cleanup emergency location
                        string emergencyPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "SMART_NOC",
                            "emergency_crash.log"
                        );
                        if (File.Exists(emergencyPath))
                            File.Delete(emergencyPath);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error checking previous crash: {ex.Message}");
                try
                {
                    CrashLogger.LogInfo($"Error checking previous crash: {ex.Message}", "CrashDetection");
                }
                catch { }
            }
        }

        // =========================================================
        // 🔥 3. EXCEPTION HANDLERS FOR DIFFERENT ERROR TYPES
        // =========================================================

        /// <summary>
        /// 🔥 XAML/UI EXCEPTIONS 🔥
        /// Triggered when exception occurs in XAML or UI thread
        /// </summary>
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                // 🔥 STEP 1: LOG EXCEPTION WITH FULL DETAILS 🔥
                CrashLogger.Log(e.Exception, "UI_THREAD", "CRITICAL");
                Debug.WriteLine($"❌ UI Exception: {e.Exception?.Message}");
                
                // Log to main window if available
                var mw = MainWindow as SMART_NOC.MainWindow;
                mw?.AddLog($"❌ UI EXCEPTION: {e.Exception?.Message}");
                
                // 🔥 STEP 2: SAVE CRASH REPORT (SYNCHRONOUS & BLOCKING) 🔥
                // This MUST complete before app force closes
                CrashLogger.SaveCrashReport(e.Exception, "UI EXCEPTION");
                
                // 🔥 STEP 3: FLUSH ALL PENDING WRITES 🔥
                // Force system to write all buffers to disk
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                
                Debug.WriteLine("🔴 UI EXCEPTION HANDLER: Crash report saved, app will terminate");
                
                // Don't handle - let app force close to save memory
                e.Handled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception in UI handler: {ex.Message}");
                try
                {
                    CrashLogger.LogInfo($"Exception in exception handler: {ex.Message}", "ExceptionHandlerError");
                }
                catch { }
            }
        }

        /// <summary>
        /// 🔥 BACKGROUND TASK EXCEPTIONS 🔥
        /// Triggered when exception occurs in async/await without await
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                // 🔥 LOG EXCEPTION 🔥
                CrashLogger.Log(e.Exception, "BACKGROUND_TASK", "ERROR");
                Debug.WriteLine($"❌ Task Exception: {e.Exception?.Message}");
                
                var mw = MainWindow as SMART_NOC.MainWindow;
                mw?.AddLog($"⚠️ TASK ERROR: {e.Exception?.Message}");
                
                // 🔥 MARK AS OBSERVED TO PREVENT FORCED CRASH 🔥
                // Background task errors should NOT force close the app
                e.SetObserved();
                
                Debug.WriteLine("✅ Task exception handled (marked as observed)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception in task handler: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 SYSTEM-LEVEL EXCEPTIONS 🔥
        /// Triggered for unhandled exceptions at AppDomain level
        /// This is the LAST resort handler before app terminates
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception? ex = e.ExceptionObject as Exception;
                
                // 🔥 STEP 1: LOG WITH FULL DETAILS 🔥
                CrashLogger.Log(ex ?? new Exception("Unknown system exception"), "SYSTEM", "CRITICAL");
                Debug.WriteLine($"❌ System Exception: {ex?.Message}");
                
                var mw = MainWindow as SMART_NOC.MainWindow;
                mw?.AddLog($"❌ SYSTEM EXCEPTION: {ex?.Message}");
                
                // 🔥 STEP 2: SAVE CRASH REPORT (MUST PERSIST) 🔥
                CrashLogger.SaveCrashReport(ex, "SYSTEM EXCEPTION");
                
                // 🔥 STEP 3: FORCE FLUSH ALL DATA TO DISK 🔥
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                
                // 🔥 STEP 4: GIVE SYSTEM TIME TO WRITE FILES 🔥
                System.Threading.Thread.Sleep(500);
                
                Debug.WriteLine("🔴 SYSTEM EXCEPTION HANDLER: App will terminate after this");
                
                // This is critical - app will terminate
                // Don't try to handle this, just let it crash
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception in system handler: {ex.Message}");
                try
                {
                    CrashLogger.LogInfo($"System handler error: {ex.Message}", "SystemHandlerError");
                }
                catch { }
            }
        }

        // =========================================================
        // 🔥 4. HELPER METHOD: Log important events 🔥
        // =========================================================
        public static void LogEvent(string message, string category = "Event")
        {
            try
            {
                CrashLogger.LogInfo(message, category);
            }
            catch { }
        }
    }
}