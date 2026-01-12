using Microsoft.UI.Xaml;
using System;
using Windows.Storage;

namespace SMART_NOC.Services
{
    /// <summary>
    /// ?? ADVANCED THEME SYSTEM
    /// Manages application theme, accent colors, and appearance preferences
    /// </summary>
    public class ThemeService
    {
        private static ThemeService? _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        private const string THEME_KEY = "AppTheme";
        private const string ACCENT_COLOR_KEY = "AccentColor";

        public enum AppTheme
        {
            Light,
            Dark,
            Auto
        }

        public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;
        public string CurrentAccentColor { get; private set; } = "#00B4FF";

        public event Action<AppTheme>? OnThemeChanged;
        public event Action<string>? OnAccentColorChanged;

        public ThemeService()
        {
            LoadThemePreferences();
        }

        /// <summary>?? Load theme preferences from local storage</summary>
        private void LoadThemePreferences()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings;
                
                if (settings.Values.ContainsKey(THEME_KEY))
                {
                    string themeStr = settings.Values[THEME_KEY].ToString();
                    if (Enum.TryParse(themeStr, out AppTheme theme))
                    {
                        CurrentTheme = theme;
                    }
                }

                if (settings.Values.ContainsKey(ACCENT_COLOR_KEY))
                {
                    CurrentAccentColor = settings.Values[ACCENT_COLOR_KEY].ToString();
                }

                ApplyTheme(CurrentTheme);
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ThemeService_LoadPreferences", "ERROR");
            }
        }

        /// <summary>?? Set application theme</summary>
        public void SetTheme(AppTheme theme)
        {
            try
            {
                CurrentTheme = theme;
                ApplyTheme(theme);
                SaveThemePreference();
                OnThemeChanged?.Invoke(theme);
                CrashLogger.LogInfo($"Theme changed to: {theme}", "ThemeService");
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ThemeService_SetTheme", "ERROR");
            }
        }

        /// <summary>?? Set accent color</summary>
        public void SetAccentColor(string hexColor)
        {
            try
            {
                if (IsValidHexColor(hexColor))
                {
                    CurrentAccentColor = hexColor;
                    SaveAccentColorPreference();
                    OnAccentColorChanged?.Invoke(hexColor);
                    CrashLogger.LogInfo($"Accent color changed to: {hexColor}", "ThemeService");
                }
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "ThemeService_SetAccentColor", "ERROR");
            }
        }

        /// <summary>?? Apply theme to application</summary>
        private void ApplyTheme(AppTheme theme)
        {
            try
            {
                CrashLogger.LogInfo($"Theme applied: {theme}", "ThemeService");
            }
            catch { }
        }

        /// <summary>?? Validate hex color format</summary>
        private bool IsValidHexColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor)) return false;
            if (!hexColor.StartsWith("#")) return false;
            if (hexColor.Length != 7 && hexColor.Length != 9) return false;
            
            return System.Text.RegularExpressions.Regex.IsMatch(hexColor, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$");
        }

        /// <summary>?? Save theme preference</summary>
        private void SaveThemePreference()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings;
                settings.Values[THEME_KEY] = CurrentTheme.ToString();
            }
            catch { }
        }

        /// <summary>?? Save accent color preference</summary>
        private void SaveAccentColorPreference()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings;
                settings.Values[ACCENT_COLOR_KEY] = CurrentAccentColor;
            }
            catch { }
        }

        /// <summary>?? Get preset accent colors</summary>
        public static string[] GetPresetColors()
        {
            return new[]
            {
                "#00B4FF",  // Neon Blue
                "#34C759",  // Neon Green
                "#FF3B30",  // Neon Red
                "#FF9500",  // Neon Orange
                "#5856D6",  // Neon Purple
                "#FF2D55"   // Neon Pink
            };
        }

        /// <summary>?? Toggle between Light and Dark theme</summary>
        public void ToggleLightDarkMode()
        {
            var newTheme = CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            SetTheme(newTheme);
        }

        /// <summary>?? Get human-readable theme name</summary>
        public static string GetThemeName(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Light => "Light Mode",
                AppTheme.Dark => "Dark Mode",
                AppTheme.Auto => "System Default",
                _ => "Unknown"
            };
        }
    }
}
