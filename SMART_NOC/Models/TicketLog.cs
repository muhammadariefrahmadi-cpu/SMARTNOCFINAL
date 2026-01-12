using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Windows.UI;

namespace SMART_NOC.Models
{
    public class TicketLog
    {
        // --- 1. Identitas Tiket ---
        public string TT_IOH { get; set; } = "";
        public string Header { get; set; } = "";
        public string TT_Mandau { get; set; } = "";

        // 🔥 NEW FEATURE: REGION (Kolom F Excel) 🔥
        public string Region { get; set; } = "";

        // --- 2. Waktu ---
        public string OccurTime { get; set; } = "";
        public string DispatchTime { get; set; } = "";
        public string ClosedTime { get; set; } = "";

        // --- 3. Lokasi & Teknis ---
        public string SegmentPM { get; set; } = "";
        public string CutPoint { get; set; } = "";
        public string RootCause { get; set; } = "";

        public string Action { get; set; } = "";

        public string Updates { get; set; } = "";
        public string Status { get; set; } = ""; // Isi: "DOWN", "UP", "FLAPPING"
        public string PicInfo { get; set; } = "";

        // --- 4. Evidence ---
        public string ImagePath { get; set; } = "";

        // --- 5. Data List ---
        public List<ImpactItem> ImpactDetail { get; set; } = new List<ImpactItem>();

        // --- 6. HELPER UI ---
        [JsonIgnore]
        public Color StatusColor
        {
            get
            {
                if (!string.IsNullOrEmpty(Status))
                {
                    string s = Status.ToUpper();
                    if (s.Contains("DOWN")) return Color.FromArgb(255, 255, 59, 48);   // Neon Red
                    if (s.Contains("UP") || s.Contains("CLOSE") || s.Contains("RESOLVE"))
                        return Color.FromArgb(255, 52, 199, 89);  // Neon Green

                    if (s.Contains("FLAPPING")) return Color.FromArgb(255, 255, 149, 0); // Neon Orange
                }
                return Color.FromArgb(255, 142, 142, 147); // Default Grey
            }
        }
    }
}