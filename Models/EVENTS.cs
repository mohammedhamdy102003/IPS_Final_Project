using System;

namespace IPS_PROJECT.Models
{
    public class EVENTS
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SourceIp { get; set; } = string.Empty;
        public string DestinationIp { get; set; } = string.Empty;
        public string TrafficType { get; set; } = string.Empty;
        public string Prediction { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
