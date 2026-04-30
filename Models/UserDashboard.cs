namespace IPS_PROJECT.Models
{
    public class UserDashboard
    {
        public int ThreatsBlockedToday { get; set; }
        public double SystemUptime { get; set; }
        public DateTime LastThreatTime { get; set; }

        public string ThreatLevelStatus { get; set; } = "Low";
        public double ThreatLevelPercentage { get; set; }
    }
}
