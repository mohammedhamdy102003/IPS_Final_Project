using IPS_PROJECT.Data;
using IPS_PROJECT.Models;
using IPS_PROJECT.Models.ViewModels;
using IPS_PROJECT.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IPS_PROJECT.Controllers
{
    [Authorize(Roles = "Admin")] // يظهر فقط للـ Admin
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Dashboard
        public async Task<IActionResult> Index()
        {
            var totalEvents = await _context.Events.CountAsync();

            var blockedThreats = await _context.Alerts
                .Include(a => a.Threat)
                .CountAsync();

            var benignTraffic = await _context.Events
                .Where(e => e.Prediction == "Benign")
                .CountAsync();

            // Recent 10 Events
            var recentEvents = await _context.Events
                .OrderByDescending(e => e.Timestamp)
                .Take(10)
                .ToListAsync();

            // Git Last 20 Notifications From AlertNotifications Table 
            var notifications = await _context.AlertNotifications
                .OrderByDescending(n => n.Timestamp)
                .Take(20)
                .ToListAsync();

            // Build ViewModel
            var viewModel = new DashboardViewModel
            {
                TotalEvents = totalEvents,
                ThreatsBlocked = blockedThreats,
                BenignTraffic = benignTraffic,
                ModelAccuracy = 0.94, // مثال ثابت، بعدين ممكن تجيبه من ModelService
                AutoBlocking = true,  // مثال، بعدين ممكن تجيبه من Configuration DB
                DetectionThreshold = 0.8,
                ModelStatus = "Active",
                RecentEvents = recentEvents,
                // For AlertNotifications
                AlertNotifications = notifications
            };

            return View(viewModel);
        }



        /// //////////////////////////////////// SEARCH FILTERS //////////////////////////////////////


        public async Task<IActionResult> Search(RecentEventsSearch search)
        {
            var query = _context.Events.AsQueryable();

            // 🔍 Smart Search (IP + Attack Type + Status)
            if (!string.IsNullOrEmpty(search.Term))
            {
                var term = search.Term.ToLower();

                query = query.Where(x =>
                    x.SourceIp.ToLower().Contains(term) ||
                    x.TrafficType.ToLower().Contains(term) ||
                    x.Status.ToLower().Contains(term)
                );
            }

            // ⏱ Time Filter
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

            var model = new DashboardViewModel
            {
                RecentEvents = await query
                    .OrderByDescending(x => x.Timestamp)
                    .ToListAsync(),

                TotalEvents = await _context.Events.CountAsync(),
                ThreatsBlocked = await _context.Events.CountAsync(x => x.Status == "Blocked"),
                BenignTraffic = await _context.Events.CountAsync(x => x.Status == "Allowed"),
                AlertNotifications = await _context.AlertNotifications.ToListAsync()
            };

            return View("Index", model); // 👈 مهم
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
