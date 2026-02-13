using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SMART_NOC.Utilities
{
    public static class CoordinateParser
    {
        private static readonly Regex DmsRegex = new(
            @"(?<latDeg>\d{1,2})\s*[°º]\s*(?<latMin>\d{1,2})\s*'\s*(?<latSec>\d{1,2}(?:\.\d+)?)\s*""?\s*(?<latHem>[NS])\s*,\s*(?<lonDeg>\d{1,3})\s*[°º]\s*(?<lonMin>\d{1,2})\s*'\s*(?<lonSec>\d{1,2}(?:\.\d+)?)\s*""?\s*(?<lonHem>[EW])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool TryParseDms(string input, out double latitude, out double longitude)
        {
            latitude = 0;
            longitude = 0;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var match = DmsRegex.Match(input.Trim());
            if (!match.Success)
            {
                return false;
            }

            if (!TryGetDouble(match.Groups["latDeg"].Value, out var latDeg) ||
                !TryGetDouble(match.Groups["latMin"].Value, out var latMin) ||
                !TryGetDouble(match.Groups["latSec"].Value, out var latSec) ||
                !TryGetDouble(match.Groups["lonDeg"].Value, out var lonDeg) ||
                !TryGetDouble(match.Groups["lonMin"].Value, out var lonMin) ||
                !TryGetDouble(match.Groups["lonSec"].Value, out var lonSec))
            {
                return false;
            }

            latitude = ToDecimal(latDeg, latMin, latSec, match.Groups["latHem"].Value);
            longitude = ToDecimal(lonDeg, lonMin, lonSec, match.Groups["lonHem"].Value);
            return true;
        }

        private static bool TryGetDouble(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static double ToDecimal(double degree, double minute, double second, string hemisphere)
        {
            var decimalValue = degree + (minute / 60.0) + (second / 3600.0);
            var hem = hemisphere.ToUpperInvariant();
            return hem == "S" || hem == "W" ? -decimalValue : decimalValue;
        }
    }
}
