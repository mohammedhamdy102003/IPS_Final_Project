using IPS_PROJECT.Data;
using IPS_PROJECT.Models;
using IPS_PROJECT.Models.ViewModels;
using IPS_PROJECT.Services;
using IPS_PROJECT.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IPS_PROJECT.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly BatchBuilderService _batchBuilder;

        public DashboardController(AppDbContext context  , BatchBuilderService batchBuilder)
        {
            _context = context;
            _batchBuilder = batchBuilder;

        }

        /*  private static string NormalizeTrafficType(string trafficType)
          {
              if (string.IsNullOrWhiteSpace(trafficType)) return "Other";

              var key = trafficType.Trim().ToUpperInvariant();
              return key switch
              {
                  "HTTP" => "HTTP/HTTPS",
                  "HTTPS" => "HTTP/HTTPS",
                  _ => trafficType.Trim()
              };
          }*/

        /* private async Task<List<AttackDistributionData>> GetAttackDistribution()
         {
             var colorMap = new Dictionary<string, string>
             {
                 { "SQL Injection", "#8833ff" },
                 { "Brute Force", "#33bfff" },
                 { "Malware", "#20c997" },
                 { "DDoS", "#ff4b5c" },
                 { "Benign", "#ff9800" },
               /*  { "FTP", "#2196f3" },
                 { "HTTP/HTTPS", "#4caf50" },
                 { "SSH", "#f44336" }*/
        /*    };

            var attackQuery = _context.Events.Where(e => !string.IsNullOrWhiteSpace(e.AttackType) && e.AttackType.Trim().ToLower() != "benign");

            var totalAttackEvents = await attackQuery.CountAsync();
            if (totalAttackEvents == 0)
            {
                return new List<AttackDistributionData>();
            }

            var distribution = attackQuery
               .GroupBy(e => e.AttackType)
               .Select(g => new AttackDistributionData
                   {
                      Type = g.Key,
                      Count = g.Count(),
                      Percentage = (int)Math.Round( g.Count() * 100.0 / totalAttackEvents)
                   }) .OrderByDescending(x => x.Count).ToList();

            var random = new Random();
            foreach (var item in distribution)
            {
                item.Color = colorMap.ContainsKey(item.Type)
                    ? colorMap[item.Type]
                    : "#" + random.Next(0x1000000).ToString("x6");
            }

            return distribution;
        }
*/
        private List<AttackDistributionData> GetBatchDistribution(List<EVENTS> batches)
        {
            var attackBatches = batches
                .Where(x => !string.IsNullOrWhiteSpace(x.AttackType) &&
                            !x.AttackType.Equals("Benign", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!attackBatches.Any())
                return new List<AttackDistributionData>();

            var total = attackBatches.Count;

            var distribution = attackBatches
                .GroupBy(x => x.AttackType)
                .Select(g => new AttackDistributionData
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Percentage = (int)Math.Round(g.Count() * 100.0 / total)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var colors = new[]
            {
        "#ef4444", "#f97316", "#eab308", "#22c55e",
        "#3b82f6", "#8b5cf6", "#6b7280"
    };

            for (int i = 0; i < distribution.Count; i++)
            {
                distribution[i].Color = i < colors.Length ? colors[i] : "#6b7280";
            }

            return distribution;
        }


        private List<EVENTS> BuildTwentyEventBatches(List<EVENTS> events)
        {
            var batches = new List<EVENTS>();

            for (var i = 0; i + 19 < events.Count; i += 20)
            {
                var batch = events.Skip(i).Take(20).ToList();

                var batchEvent = _batchBuilder.BuildBatch(batch);

                batches.Add(batchEvent);
            }

            return batches
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }
                /*var latest = batch.OrderByDescending(e => e.Timestamp).First();
                var benignCount = batch.Count(e => e.AttackType.Equals("Benign", StringComparison.OrdinalIgnoreCase));
                var attackCount = batch.Count - benignCount;


                string batchPrediction;
                double confidence;
                string status;
                string Is_Anomaly;

                if (benignCount >= attackCount)
                {
                    batchPrediction = "Benign";
                    confidence = batch
                        .Where(e => e.AttackType.Equals("Benign", StringComparison.OrdinalIgnoreCase))
                        .DefaultIfEmpty(latest)
                        .Average(e => e.Confidence);
                    status = "Allowed";
                    Is_Anomaly = " Not Anomaly";
                }
                else
                {
                    var topAttack = batch
                        .Where(e => !e.AttackType.Equals("Benign", StringComparison.OrdinalIgnoreCase))
                        .GroupBy(e => e.AttackType)
                        .OrderByDescending(g => g.Count())
                        .ThenByDescending(g => g.Average(e => e.Confidence))
                        .First();

                    batchPrediction = topAttack.Key;
                    confidence = topAttack.Average(e => e.Confidence);
                    status = "Blocked";
                    Is_Anomaly = " Anomaly";
                }

                batches.Add(new EVENTS
                {
                    Id = latest.Id,
                    Timestamp = latest.Timestamp,
                    SourceIp = latest.SourceIp,
                    DestinationIp = latest.DestinationIp,
                    Prediction = Is_Anomaly ,
                    AttackType = batchPrediction,
                    Confidence = Math.Round(confidence, 1),
                    Status = status
                });
            }*/

             

          

        private async Task<DashboardViewModel> BuildDashboardViewModel(IQueryable<EVENTS>? query = null)
        {
            var baseQuery = query ?? _context.Events;

            /* var recentEvents = await baseQuery
                 .OrderByDescending(e => e.Timestamp)
                 .Take(20)
                 .ToListAsync();*/

            var allEvents = await baseQuery
                 .OrderBy(e => e.Id)
                 .ToListAsync();

            var batches = BuildTwentyEventBatches(allEvents);

            var recentEvents = batches
                .OrderByDescending(x => x.Timestamp)
                .Take(20)
                .ToList();

            var attackDistribution =  GetBatchDistribution(batches);

            var notifications = await _context.AlertNotifications
                .OrderByDescending(n => n.Timestamp)
                .Take(20)
                .ToListAsync();

            var modelIntelligence = new ModelIntelligenceData
            {
                Status = "Active",
                Version = "v3.6.1-stable",
                LastTraining = DateTime.Now.AddDays(-5),
                Parameters = "1.4B weights",
                AlertMessage = "New threat patterns detected in the last 6 hours suggest a potential model update is required for optimal protection."
            };

            return new DashboardViewModel
            {
                TotalEvents = batches.Count,
                ThreatsBlocked = batches.Count(x => x.Status == "Blocked"),
                BenignTraffic = batches.Count(x => x.Status == "Allowed"),
                ModelAccuracy = 0.94,
                AutoBlocking = true,
                DetectionThreshold = 0.8,
                ModelStatus = "Active",
                RecentEvents = recentEvents,
                AttackDistribution = attackDistribution,
                ModelIntelligence = modelIntelligence,
                AlertNotifications = notifications
            };
        }

        public async Task<IActionResult> Index()
        {
            var model = await BuildDashboardViewModel();
            return View(model);
        }

        public async Task<IActionResult> Search(RecentEventsSearch search)
        {
            var query = _context.Events.AsQueryable();

            if (!string.IsNullOrEmpty(search.Term))
            {
                var term = search.Term.ToLower();
                query = query.Where(x =>
                    (x.SourceIp != null && x.SourceIp.ToLower().Contains(term)) ||
                    (x.AttackType != null && x.AttackType.ToLower().Contains(term)) ||
                    (x.Status != null && x.Status.ToLower().Contains(term))
                );
            }

            if (!string.IsNullOrEmpty(search.TimeRange))
            {
                var now = DateTime.UtcNow;
                switch (search.TimeRange)
                {
                    case "24h":
                        query = query.Where(x => x.Timestamp >= now.AddHours(-24));
                        break;
                    case "7d":
                        query = query.Where(x => x.Timestamp >= now.AddDays(-7));
                        break;
                    case "30d":
                        query = query.Where(x => x.Timestamp >= now.AddDays(-30));
                        break;
                }
            }

            var model = await BuildDashboardViewModel(query);
            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAttackDistributionData()
        {
            var allEvents = await _context.Events
                .OrderBy(e => e.Id)
                .ToListAsync();

            var batches = BuildTwentyEventBatches(allEvents);

            var data = GetBatchDistribution(batches);

            return Json(data);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminRequests()
        {
            var requests = await _context.AdminRequests
                .Include(r => r.User)
                .Where(r => r.Status == "Pending")
                .ToListAsync();

            return View(requests);
        }
    }
}