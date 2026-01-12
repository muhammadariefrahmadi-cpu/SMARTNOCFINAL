namespace SMART_NOC.Constants;

/// <summary>
/// Application-wide constants for window and UI configuration
/// </summary>
public static class WindowConstants
{
    public const string MainWindowTitle = "SMART NOC COMMANDER";
    public const string MainWindowPersistenceId = "SmartNocMainWindow";
    public const int DefaultMinWidth = 1000;
    public const int DefaultMinHeight = 700;
}

/// <summary>
/// Constants for messaging and logging
/// </summary>
public static class MessageConstants
{
    public const string SystemInitialized = "System Initialized. Ready for command.";
    public const string ActivityMonitoringEnabled = "?? Activity monitoring enabled - all interactions tracked";
    public const string ProcessKilled = "?? USER REQUESTED TO KILL PROCESS.";
    public const string ForceCloseDetected = "?? FORCE CLOSE TERDETEKSI";
    public const string LogsCleared = "Logs cleared.";
    public const string LogsCopied = "All logs copied to clipboard.";
    public const string LogFileNotFound = "Log file not found yet.";
}

/// <summary>
/// Constants for navigation and page tags
/// </summary>
public static class NavigationConstants
{
    public const string DashboardTag = "Dashboard";
    public const string CreateTicketTag = "CreateTicket";
    public const string LiveMapTag = "LiveMap";
    public const string HistoryTag = "History";
    public const string HandoverLogTag = "HandoverLog";
    public const string SettingsTag = "Settings";
}

/// <summary>
/// Constants for activity monitoring thresholds
/// </summary>
public static class ActivityMonitorConstants
{
    public const double MouseFrozenSpeedThreshold = 0.01;      // px/ms
    public const double MouseMoveDistanceThreshold = 100;      // pixels
    public const double MinDragDistance = 5;                   // pixels
    public const double MinWindowDragDistance = 10;            // pixels
    public const double MinDragSuccessRate = 0.5;              // 50%
}

/// <summary>
/// Constants for window drag tracking
/// </summary>
public static class DragTrackingConstants
{
    public const double DragStartThreshold = 0.5;              // pixels
    public const int DragDebugUpdateInterval = 100;            // milliseconds
}
