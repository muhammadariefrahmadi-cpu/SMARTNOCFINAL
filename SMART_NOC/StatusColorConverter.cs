using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace SMART_NOC.Views
{
    // Pastikan namespace-nya sama dengan "local:" di XAML (SMART_NOC.Views)
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string status = value as string;
            if (string.IsNullOrEmpty(status)) return new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)); // Default Grey

            status = status.ToUpper();
            if (status.Contains("DOWN")) return new SolidColorBrush(Color.FromArgb(50, 239, 68, 68)); // Red BG (Transparent)
            if (status.Contains("UP") || status.Contains("RESOLVE") || status.Contains("CLOSE"))
                return new SolidColorBrush(Color.FromArgb(50, 34, 197, 94)); // Green BG (Transparent)

            return new SolidColorBrush(Color.FromArgb(50, 234, 179, 8)); // Yellow/Warning
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}