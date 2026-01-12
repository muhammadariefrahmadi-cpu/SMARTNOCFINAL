using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SMART_NOC.Services
{
    /// <summary>
    /// 🔥 COMPREHENSIVE CRASH LOGGER 🔥
    /// Menangkap SEMUA jenis error tanpa terkecuali:
    /// - UI Exceptions
    /// - Background Task Errors
    /// - System-level Exceptions
    /// - Network Errors
    /// - File I/O Errors
    /// - Resource Errors
    /// </summary>
    public static class CrashLogger
    {
        // 🔥 THREAD-SAFE LOCKING 🔥
        private static readonly object _lock = new object();

        // 🔥 CRASH SESSION TRACKING 🔥
        private static string _sessionId = Guid.NewGuid().ToString().Substring(0, 8);
        private static int _errorCount = 0;
        private static DateTime _sessionStart = DateTime.Now;

        // 🔥 ERROR CATEGORIES 🔥
        private static readonly HashSet<string> _capturedErrors = new HashSet<string>();

        private static string GetLogPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "SMART_NOC", "Logs");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return Path.Combine(folder, "crash_log.txt");
        }

        private static string GetCrashReportPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "SMART_NOC");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return Path.Combine(folder, "last_crash.txt");
        }

        private static string GetSessionLogPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "SMART_NOC", "Logs");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"session_{_sessionId}.log");
        }

        /// <summary>
        /// 🔥 LOG EXCEPTION WITH FULL CONTEXT 🔥
        /// Captures: Type, Message, Stack Trace, Inner Exception, Source, HResult
        /// </summary>
        public static void Log(Exception ex, string context = "General", string category = "ERROR")
        {
            if (ex == null) return;

            lock (_lock)
            {
                try
                {
                    _errorCount++;
                    string errorId = $"{_errorCount:D4}";
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string exceptionType = ex.GetType().FullName ?? "Unknown";

                    // 🔥 BUILD COMPREHENSIVE ERROR REPORT 🔥
                    StringBuilder report = new StringBuilder();
                    report.AppendLine();
                    report.AppendLine("════════════════════════════════════════════════════════════════");
                    report.AppendLine($"[ERROR #{errorId}] {timestamp}");
                    report.AppendLine($"Category: {category}");
                    report.AppendLine($"Context: {context}");
                    report.AppendLine($"Exception Type: {exceptionType}");
                    report.AppendLine("════════════════════════════════════════════════════════════════");
                    
                    report.AppendLine($"Message: {ex.Message}");
                    report.AppendLine($"Source: {ex.Source}");
                    report.AppendLine($"HResult: {ex.HResult:X}");
                    
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        report.AppendLine("Stack Trace:");
                        report.AppendLine(ex.StackTrace);
                    }

                    // 🔥 NESTED EXCEPTIONS 🔥
                    if (ex.InnerException != null)
                    {
                        report.AppendLine();
                        report.AppendLine("─── INNER EXCEPTION ───");
                        report.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                        report.AppendLine($"Message: {ex.InnerException.Message}");
                        if (!string.IsNullOrEmpty(ex.InnerException.StackTrace))
                            report.AppendLine($"Stack: {ex.InnerException.StackTrace}");
                    }

                    // 🔥 AGGREGATE EXCEPTIONS 🔥
                    if (ex is AggregateException aggEx)
                    {
                        report.AppendLine();
                        report.AppendLine("─── AGGREGATE INNER EXCEPTIONS ───");
                        int idx = 0;
                        foreach (var innerEx in aggEx.InnerExceptions)
                        {
                            idx++;
                            report.AppendLine($"[{idx}] {innerEx.GetType().FullName}: {innerEx.Message}");
                        }
                    }

                    // 🔥 SYSTEM INFO 🔥
                    report.AppendLine();
                    report.AppendLine("─── SYSTEM INFO ───");
                    report.AppendLine($"OS: {Environment.OSVersion}");
                    report.AppendLine($"Platform: {RuntimeInformation.OSDescription}");
                    report.AppendLine($"Process ID: {Process.GetCurrentProcess().Id}");
                    report.AppendLine($"Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                    try
                    {
                        report.AppendLine($"Available Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
                    }
                    catch { }

                    report.AppendLine("════════════════════════════════════════════════════════════════");

                    // 🔥 APPEND TO MAIN LOG 🔥
                    string logPath = GetLogPath();
                    File.AppendAllText(logPath, report.ToString());

                    // 🔥 APPEND TO SESSION LOG 🔥
                    string sessionPath = GetSessionLogPath();
                    File.AppendAllText(sessionPath, report.ToString());

                    // 🔥 TRACK CAPTURED ERRORS 🔥
                    _capturedErrors.Add($"{errorId}_{exceptionType}");

                    // 🔥 OUTPUT TO DEBUG CONSOLE 🔥
                    Debug.WriteLine(report.ToString());
                }
                catch (Exception logEx)
                {
                    // 🔥 EMERGENCY FALLBACK: TRY TO SAVE CRASH INFO 🔥
                    try
                    {
                        string emergencyPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "SMART_NOC",
                            "emergency.log"
                        );
                        File.AppendAllText(emergencyPath, $"[LOGGER FAILED] {logEx.Message}\n");
                    }
                    catch { /* Final fallback */ }
                }
            }
        }

        /// <summary>
        /// 🔥 LOG WITHOUT EXCEPTION (For informational messages) 🔥
        /// </summary>
        public static void LogInfo(string message, string context = "Info")
        {
            lock (_lock)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] [{context}] {message}\n";

                    string logPath = GetLogPath();
                    File.AppendAllText(logPath, logEntry);

                    string sessionPath = GetSessionLogPath();
                    File.AppendAllText(sessionPath, logEntry);

                    Debug.WriteLine(logEntry);
                }
                catch { /* Silent fail */ }
            }
        }

        /// <summary>
        /// 🔥 SAVE CRASH REPORT FOR NEXT STARTUP (SYNCHRONOUS & PERSISTENT) 🔥
        /// Called before force close to show error on restart
        /// MUST save synchronously before app terminates
        /// </summary>
        public static void SaveCrashReport(Exception ex, string title = "APPLICATION CRASH")
        {
            lock (_lock)
            {
                try
                {
                    StringBuilder report = new StringBuilder();
                    report.AppendLine($"╔══════════════════════════════════════════╗");
                    report.AppendLine($"║  ❌ {title}");
                    report.AppendLine($"╚══════════════════════════════════════════╝");
                    report.AppendLine();
                    report.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    report.AppendLine($"Session ID: {_sessionId}");
                    report.AppendLine($"Error Count: {_errorCount}");
                    report.AppendLine();
                    report.AppendLine("─── LAST ERROR ───");
                    report.AppendLine($"Type: {ex?.GetType().FullName ?? "Unknown"}");
                    report.AppendLine($"Message: {ex?.Message ?? "No message"}");
                    report.AppendLine();
                    report.AppendLine("Stack Trace:");
                    report.AppendLine(ex?.StackTrace ?? "No stack trace");
                    report.AppendLine();
                    report.AppendLine("For full logs, check:");
                    report.AppendLine(GetLogPath());
                    report.AppendLine();

                    string crashReportPath = GetCrashReportPath();
                    
                    // 🔥 SYNCHRONOUS WRITE - MUST COMPLETE BEFORE APP TERMINATES 🔥
                    // Ensure folder exists
                    string folder = Path.GetDirectoryName(crashReportPath);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    
                    // Write with retry logic
                    int retries = 3;
                    while (retries > 0)
                    {
                        try
                        {
                            // Use StreamWriter for more control over flushing
                            using (var writer = new StreamWriter(crashReportPath, false, Encoding.UTF8, 4096))
                            {
                                writer.Write(report.ToString());
                                writer.Flush();
                                writer.Close();
                            }
                            
                            // 🔥 VERIFY FILE WAS WRITTEN 🔥
                            if (File.Exists(crashReportPath))
                            {
                                Debug.WriteLine($"✅ Crash report saved: {crashReportPath}");
                                break;
                            }
                        }
                        catch (Exception writeEx)
                        {
                            retries--;
                            Debug.WriteLine($"⚠️ Crash report write failed (attempt {4 - retries}): {writeEx.Message}");
                            
                            if (retries == 0)
                            {
                                // Last resort: write to emergency log
                                try
                                {
                                    string emergencyPath = Path.Combine(
                                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                        "SMART_NOC",
                                        "emergency_crash.log"
                                    );
                                    File.WriteAllText(emergencyPath, report.ToString());
                                    Debug.WriteLine($"✅ Emergency crash report saved: {emergencyPath}");
                                }
                                catch { }
                            }
                        }
                    }

                    // 🔥 ALSO SAVE TO MAIN LOG FOR REFERENCE 🔥
                    try
                    {
                        string logPath = GetLogPath();
                        File.AppendAllText(logPath, $"\n\n🔴 CRASH REPORT TRIGGERED: {title}\n{report.ToString()}\n");
                    }
                    catch { }
                }
                catch (Exception logEx)
                {
                    Debug.WriteLine($"❌ SaveCrashReport failed completely: {logEx.Message}");
                }
            }
        }

        /// <summary>
        /// 🔥 READ PREVIOUS CRASH REPORT 🔥
        /// Called on startup to show error that happened before
        /// </summary>
        public static string ReadCrashReport()
        {
            try
            {
                string path = GetCrashReportPath();
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 🔥 READ FULL CRASH LOG 🔥
        /// </summary>
        public static string ReadLog()
        {
            try
            {
                string path = GetLogPath();
                if (File.Exists(path))
                {
                    // 🔥 READ LAST 100 LINES ONLY (avoid memory issues) 🔥
                    var allLines = File.ReadAllLines(path);
                    int startLine = Math.Max(0, allLines.Length - 100);
                    return string.Join("\n", allLines.Skip(startLine));
                }
            }
            catch { }
            return "No logs available";
        }

        /// <summary>
        /// 🔥 READ CURRENT SESSION LOG 🔥
        /// </summary>
        public static string ReadSessionLog()
        {
            try
            {
                string path = GetSessionLogPath();
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
            }
            catch { }
            return "No session log available";
        }

        /// <summary>
        /// 🔥 GET SESSION STATISTICS 🔥
        /// </summary>
        public static string GetSessionStats()
        {
            StringBuilder stats = new StringBuilder();
            stats.AppendLine($"Session ID: {_sessionId}");
            stats.AppendLine($"Start Time: {_sessionStart:yyyy-MM-dd HH:mm:ss}");
            stats.AppendLine($"Duration: {DateTime.Now - _sessionStart:hh\\:mm\\:ss}");
            stats.AppendLine($"Total Errors: {_errorCount}");
            stats.AppendLine($"Unique Error Types: {_capturedErrors.Count}");
            return stats.ToString();
        }

        /// <summary>
        /// 🔥 CLEAR LOGS 🔥
        /// </summary>
        public static void ClearLog()
        {
            try
            {
                string path = GetLogPath();
                if (File.Exists(path)) File.Delete(path);
                
                string crashPath = GetCrashReportPath();
                if (File.Exists(crashPath)) File.Delete(crashPath);

                LogInfo("Logs cleared", "Admin");
            }
            catch { }
        }

        /// <summary>
        /// 🔥 GET LOG FILE PATHS 🔥
        /// </summary>
        public static string GetLogFilePath() => GetLogPath();
        public static string GetSessionId() => _sessionId;
        public static int GetErrorCount() => _errorCount;
    }
}