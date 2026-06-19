using System;
using System.Collections.Generic;
using IPS_PROJECT.Models;
using IPS_PROJECT.Models.ViewModels;

namespace IPS_PROJECT.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int ThreatsBlocked { get; set; }
        public int BenignTraffic { get; set; }
        public double ModelAccuracy { get; set; }

        public RecentEventsSearch Search { get; set; }

        public bool AutoBlocking { get; set; }
        public double DetectionThreshold { get; set; }
        public string ModelStatus { get; set; } = "Active";

        public List<EVENTS> RecentEvents { get; set; } = new();
        public List<AttackDistributionData> AttackDistribution { get; set; } = new();
        public ModelIntelligenceData ModelIntelligence { get; set; } = new();
        public List<AlertNotification> AlertNotifications { get; set; } = new List<AlertNotification>();
    }

    public class AttackDistributionData
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class ModelIntelligenceData
    {
        public string Status { get; set; } = "Active";
        public string Version { get; set; } = "v3.6.1-stable";
        public DateTime LastTraining { get; set; }
        public string Parameters { get; set; } = "1.4B weights";
        public string AlertMessage { get; set; } = string.Empty;
    }
}