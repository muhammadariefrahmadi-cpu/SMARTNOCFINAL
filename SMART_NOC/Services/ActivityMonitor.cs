using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;
using Windows.System;
using Windows.Foundation;
using Microsoft.UI;
using Windows.Devices.Input;

namespace SMART_NOC.Services
{
    /// <summary>
    /// ?? COMPREHENSIVE ACTIVITY MONITOR ??
    /// Tracks ALL user interactions, window movements, and anomalies
    /// Logs everything to debug console with timestamp, coordinates, and anomaly detection
    /// </summary>
    public class ActivityMonitor
    {
        private static ActivityMonitor? _instance;
        public static ActivityMonitor Instance => _instance ??= new ActivityMonitor();

        // ?? Tracking state
        private Point _lastMousePosition = new Point(0, 0);
        private DateTime _lastMouseMoveTime = DateTime.Now;
        private int _dragAttempts = 0;
        private int _successfulDrags = 0;
        private bool _isDragging = false;
        private Point _dragStartPosition = new Point(0, 0);
        private DateTime _dragStartTime = DateTime.Now;

        // ?? Anomaly detection
        private int _anomalyCount = 0;
        private string _lastAnomalyType = "";
        private DateTime _lastAnomalyTime = DateTime.Now;

        public event Action<string>? OnAnomalyDetected;

        public ActivityMonitor()
        {
            CrashLogger.LogInfo("ActivityMonitor initialized", "ActivityMonitor");
        }

        /// <summary>
        /// ?? Register window for mouse tracking
        /// </summary>
        public void RegisterWindow(Window window)
        {
            if (window == null) return;

            try
            {
                window.Content?.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved), true);
                window.Content?.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);
                window.Content?.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnPointerReleased), true);
                
                CrashLogger.LogInfo("Window registered for activity monitoring", "ActivityMonitor");
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ActivityMonitor_RegisterWindow", "ERROR");
            }
        }

        /// <summary>
        /// ?? LOG POINTER MOVE (Mouse move tracking)
        /// </summary>
        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                Point currentPosition = e.GetCurrentPoint(null).Position;
                DateTime now = DateTime.Now;
                double distance = CalculateDistance(_lastMousePosition, currentPosition);
                double moveSpeed = distance / (now - _lastMouseMoveTime).TotalMilliseconds; // px/ms

                // ?? Log mouse movement with coordinates
                string logEntry = $"???  MOUSE MOVE [X: {currentPosition.X:F0}, Y: {currentPosition.Y:F0}] " +
                                $"Distance: {distance:F1}px Speed: {moveSpeed:F2}px/ms";
                
                MainWindow.Instance?.AddLog(logEntry);
                CrashLogger.LogInfo(logEntry, "MouseMove");

                _lastMousePosition = currentPosition;
                _lastMouseMoveTime = now;

                // ?? DETECT ANOMALY: Sudden stops (mouse frozen)
                if (distance > 100 && moveSpeed < 0.01) // Moved far but too slow = anomaly
                {
                    ReportAnomaly("MOUSE_FROZEN", $"Mouse moved {distance:F1}px but speed is {moveSpeed:F2}px/ms (very slow)");
                }
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ActivityMonitor_OnPointerMoved", "ERROR");
            }
        }

        /// <summary>
        /// ?? LOG POINTER PRESSED (Mouse down/drag start)
        /// </summary>
        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                Point position = e.GetCurrentPoint(null).Position;
                
                _dragStartPosition = position;
                _dragStartTime = DateTime.Now;
                _isDragging = true;
                _dragAttempts++;

                string logEntry = $"?? MOUSE DOWN [X: {position.X:F0}, Y: {position.Y:F0}] " +
                                $"DragAttempt: {_dragAttempts}";
                
                MainWindow.Instance?.AddLog(logEntry);
                CrashLogger.LogInfo(logEntry, "MouseDown");
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ActivityMonitor_OnPointerPressed", "ERROR");
            }
        }

        /// <summary>
        /// ?? LOG POINTER RELEASED (Mouse up/drag end)
        /// </summary>
        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                if (!_isDragging) return;

                Point endPosition = e.GetCurrentPoint(null).Position;
                TimeSpan dragDuration = DateTime.Now - _dragStartTime;
                double dragDistance = CalculateDistance(_dragStartPosition, endPosition);

                _isDragging = false;

                // ?? Detect successful drag
                bool isDragSuccessful = dragDistance > 5; // Must move at least 5px to count as drag
                if (isDragSuccessful)
                {
                    _successfulDrags++;
                }

                // ?? DETAILED DRAG DIAGNOSTICS ??
                string dragDiagnostics = isDragSuccessful 
                    ? $"? SUCCESS" 
                    : $"? FAIL (only {dragDistance:F1}px moved, threshold=5px)";

                string logEntry = $"?? MOUSE UP [X: {endPosition.X:F0}, Y: {endPosition.Y:F0}] " +
                                $"Duration: {dragDuration.TotalMilliseconds:F0}ms " +
                                $"Distance: {dragDistance:F1}px " +
                                $"Result: {dragDiagnostics} " +
                                $"Ratio: {_successfulDrags}/{_dragAttempts}";
                
                MainWindow.Instance?.AddLog(logEntry);
                CrashLogger.LogInfo(logEntry, "MouseUp");

                // ?? DETAILED ANOMALY REPORTING ??
                double dragSuccessRate = _dragAttempts > 0 ? (_successfulDrags / (double)_dragAttempts) : 0;
                
                if (_dragAttempts > 3 && dragSuccessRate < 0.5)
                {
                    string anomalyDetails = 
                        $"Success Rate: {dragSuccessRate * 100:F0}% " +
                        $"({_successfulDrags}/{_dragAttempts})\n" +
                        $"Last Drag: {dragDistance:F1}px distance in {dragDuration.TotalMilliseconds:F0}ms\n" +
                        $"Start: [{_dragStartPosition.X:F0}, {_dragStartPosition.Y:F0}] " +
                        $"End: [{endPosition.X:F0}, {endPosition.Y:F0}]";
                    
                    ReportAnomaly("DRAG_FAILURE", anomalyDetails);
                }
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ActivityMonitor_OnPointerReleased", "ERROR");
            }
        }

        /// <summary>
        /// ?? LOG WINDOW POSITION CHANGE
        /// </summary>
        public void LogWindowPosition(int x, int y, int width, int height)
        {
            try
            {
                string logEntry = $"?? WINDOW POS [X: {x}, Y: {y}, W: {width}, H: {height}]";
                MainWindow.Instance?.AddLog(logEntry);
                CrashLogger.LogInfo(logEntry, "WindowPosition");
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ActivityMonitor_LogWindowPosition", "ERROR");
            }
        }

        /// <summary>
        /// ?? LOG CONTROL INTERACTION
        /// </summary>
        public void LogControlInteraction(string controlName, string eventType, string details = "")
        {
            try
            {
                string logEntry = $"???  CONTROL [{controlName}] {eventType} {details}";
                MainWindow.Instance?.AddLog(logEntry);
                CrashLogger.LogInfo(logEntry, "ControlInteraction");
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ActivityMonitor_LogControlInteraction", "ERROR");
            }
        }

        /// <summary>
        /// ?? LOG NAVIGATION EVENT
        /// </summary>
        public void LogNavigation(string fromPage, string toPage)
        {
            try
            {
                string logEntry = $"?? NAVIGATION [{fromPage}] ? [{toPage}]";
                MainWindow.Instance?.AddLog(logEntry);
                CrashLogger.LogInfo(logEntry, "Navigation");
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ActivityMonitor_LogNavigation", "ERROR");
            }
        }

        /// <summary>
        /// ?? LOG TITLE BAR DRAG (Window drag tracking)
        /// </summary>
        public void LogTitleBarDragAttempt(string status, Point startPos, Point endPos, double duration)
        {
            try
            {
                double distance = CalculateDistance(startPos, endPos);
                bool isDragSuccessful = distance > 10; // Window drag needs > 10px to be effective

                string logEntry = isDragSuccessful
                    ? $"?? TITLEBAR DRAG ? [{startPos.X:F0},{startPos.Y:F0}] ? [{endPos.X:F0},{endPos.Y:F0}] " +
                      $"Distance: {distance:F1}px Duration: {duration:F0}ms"
                    : $"?? TITLEBAR DRAG ? [{startPos.X:F0},{startPos.Y:F0}] ? [{endPos.X:F0},{endPos.Y:F0}] " +
                      $"Distance: {distance:F1}px (need >10px) Duration: {duration:F0}ms";

                MainWindow.Instance?.AddLog(logEntry);
                CrashLogger.LogInfo(logEntry, "TitleBarDrag");

                if (!isDragSuccessful)
                {
                    ReportAnomaly("TITLEBAR_DRAG_FAIL", 
                        $"Window drag moved only {distance:F1}px (threshold: 10px)\n" +
                        $"Start: [{startPos.X:F0}, {startPos.Y:F0}] End: [{endPos.X:F0}, {endPos.Y:F0}]");
                }
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ActivityMonitor_LogTitleBarDragAttempt", "ERROR");
            }
        }

        /// <summary>
        /// ?? DETECT AND REPORT ANOMALIES
        /// </summary>
        private void ReportAnomaly(string anomalyType, string description)
        {
            try
            {
                _anomalyCount++;
                _lastAnomalyType = anomalyType;
                _lastAnomalyTime = DateTime.Now;

                string anomalyMessage = $"?? ANOMALY #{_anomalyCount} [{anomalyType}] {description}";
                
                // ?? Log to console
                MainWindow.Instance?.AddLog($"?? {anomalyMessage}");
                CrashLogger.Log(
                    new Exception(anomalyMessage), 
                    anomalyType, 
                    "ANOMALY"
                );

                // ?? Trigger anomaly event
                OnAnomalyDetected?.Invoke(anomalyMessage);
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ActivityMonitor_ReportAnomaly", "ERROR");
            }
        }

        /// <summary>
        /// ?? GET STATISTICS
        /// </summary>
        public string GetStatistics()
        {
            double dragSuccessRate = _dragAttempts > 0 ? (_successfulDrags / (double)_dragAttempts * 100) : 0;
            
            return $"?? ACTIVITY STATS\n" +
                   $"  Drag Attempts: {_dragAttempts}\n" +
                   $"  Successful Drags: {_successfulDrags}\n" +
                   $"  Success Rate: {dragSuccessRate:F1}%\n" +
                   $"  Total Anomalies: {_anomalyCount}\n" +
                   $"  Last Anomaly: {_lastAnomalyType} @ {_lastAnomalyTime:HH:mm:ss}";
        }

        /// <summary>
        /// ?? RESET STATISTICS
        /// </summary>
        public void ResetStatistics()
        {
            _dragAttempts = 0;
            _successfulDrags = 0;
            _anomalyCount = 0;
            _lastAnomalyType = "";
            MainWindow.Instance?.AddLog("?? Activity statistics reset");
            CrashLogger.LogInfo("Activity statistics reset", "ActivityMonitor");
        }

        /// <summary>
        /// ?? CALCULATE DISTANCE BETWEEN TWO POINTS
        /// </summary>
        private double CalculateDistance(Point p1, Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// ?? GET DETAILED REPORT
        /// </summary>
        public string GetDetailedReport()
        {
            return $"???????????????????????????????????????????????????????????\n" +
                   $"?? COMPREHENSIVE ACTIVITY REPORT\n" +
                   $"???????????????????????????????????????????????????????????\n" +
                   $"{GetStatistics()}\n" +
                   $"Last Mouse Position: X: {_lastMousePosition.X:F0}, Y: {_lastMousePosition.Y:F0}\n" +
                   $"Monitoring Active: {(_isDragging ? "?? DRAGGING IN PROGRESS" : "?? IDLE")}\n" +
                   $"???????????????????????????????????????????????????????????";
        }
    }
}
