namespace IPS_PROJECT.Models
{
    public class ALERTS
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;

        public int ThreatId { get; set; }
        public THREATS Threat { get; set; } = null!;       // راجعي
    }
}


