using IPS_PROJECT.Models;
using IPS_PROJECT.Models.ViewModels;
using System.Collections.Generic;

namespace IPS_PROJECT.ViewModels
{
    public class DashboardViewModel
    {
        // Summary Cards

        public int TotalEvents { get; set; }
        public int ThreatsBlocked { get; set; }
        public int BenignTraffic { get; set; }
        public double ModelAccuracy { get; set; }

        //Events search VM 

        public RecentEventsSearch Search { get; set; }


        // System Configuration
        public bool AutoBlocking { get; set; }
        public double DetectionThreshold { get; set; }
        public string ModelStatus { get; set; } = "Active";

        // Recent Events Table
        public List<EVENTS> RecentEvents { get; set; } = new();

        // AlertNotifications Table
        public List<AlertNotification> AlertNotifications { get; set; } = new List<AlertNotification>();
    }
}
