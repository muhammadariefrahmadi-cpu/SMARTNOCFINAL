using System;

namespace SMART_NOC.Models
{
    public class HandoverLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string OperatorName { get; set; } = "NOC Operator"; // Bisa diganti nanti
        public string Message { get; set; } = "";
        public bool IsImportant { get; set; } = false; // Tandai merah kalau penting
    }
}