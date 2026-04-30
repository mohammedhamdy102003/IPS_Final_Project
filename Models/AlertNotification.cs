using System;
using System.ComponentModel.DataAnnotations;

namespace IPS_PROJECT.Models
{
    public class AlertNotification
    {
        [Key]
        public int Id { get; set; }

       
        public string AttackType { get; set; } = string.Empty;
        public string SourceIp { get; set; } = string.Empty;
        public string DestinationIp { get; set; } = string.Empty;
        public string Prediction { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Protocol { get; set; } = "TCP/IP";
        public string Status { get; set; } = "Blocked";

        public DateTime Timestamp { get; set; } = DateTime.Now;

        
        public bool IsRead { get; set; } = false;
    }
}