using IPS_PROJECT.Data;
using IPS_PROJECT.Models;
using IPS_PROJECT.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IPS_PROJECT.Controllers
{
    [Authorize] // أي مستخدم مسجل دخول
    public class UserDashboardController : Controller
    {
        private readonly AppDbContext _context;

        // Constructor Injection
        public UserDashboardController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index()
        {
            var isNotSecure = await _context.Events
                .AnyAsync(e =>
                    (e.Prediction ?? "").ToLower() != "benign"
                    && e.Status == "Allowed");

            var model = new UserDashboardViewModel
            {
                IsSecure = !isNotSecure
            };

            return View(model);
        }
    }



    /* var totalEvents = await _context.Events.CountAsync();

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
         .ToListAsync();*/
    // احصائيات
    /*  var today = DateTime.Today;
      var threatsBlocked = await _context.Alerts
          .Include(a => a.Threat)
          .CountAsync(a => a.Threat != null && a.Threat.DetectedTime.Date == today);

      var lastThreat = await _context.Threats
          .OrderByDescending(t => t.DetectedTime)
          .FirstOrDefaultAsync();

      // حساب Threat Level مثال
      int totalEventsToday = await _context.Events.CountAsync(e => e.Timestamp.Date == today);
      double threatLevel = totalEventsToday == 0 ? 0 : (double)threatsBlocked / totalEventsToday * 100;
      string threatStatus = threatLevel < 30 ? "Low" : (threatLevel < 70 ? "Medium" : "High");

      var dashboardData = new UserDashboard
      {
          ThreatsBlockedToday = threatsBlocked,
          SystemUptime = 99.8, // مثال ثابت، ممكن تجيبه من service
          LastThreatTime = lastThreat?.DetectedTime ?? DateTime.Now,
          ThreatLevelStatus = threatStatus,
          ThreatLevelPercentage = threatLevel
      };*/



}


