using System;

namespace IPS_PROJECT.Models
{
    public class THREATS
    {
        public int Id { get; set; }
        public string AttackType { get; set; } = string.Empty;
        public DateTime DetectedTime { get; set; } = DateTime.Now;
        public string Severity { get; set; } = string.Empty;

        public ALERTS? Alert { get; set; } // One-to-One, optional reference
    }
}
