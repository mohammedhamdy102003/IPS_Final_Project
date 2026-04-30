namespace IPS_PROJECT.Models
{
    public class TrafficIncomingRequest
    {
        public string? source_ip { get; set; }
        public string? destination_ip { get; set; }
        public int protocol { get; set; }
        // استخدام object للسماح باستقبال true/false من السنيفر
        public Dictionary<string, object>? data { get; set; }
    }
}