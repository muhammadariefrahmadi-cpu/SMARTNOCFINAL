using System;

namespace SMART_NOC.Models
{
    public class TransmissionTicket
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string OccurTime { get; set; } = string.Empty;
        public string DispatchTime { get; set; } = string.Empty;
        public string Pic { get; set; } = string.Empty;
        public string RootCause { get; set; } = string.Empty;
        public string CutPoint { get; set; } = string.Empty;
        public string CoordinateDms { get; set; } = string.Empty;
        public string UpdateProgress { get; set; } = string.Empty;
        public string TemplatePreview { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedAtDisplay => CreatedAt.ToString("dd-MM-yyyy HH:mm");
    }
}

