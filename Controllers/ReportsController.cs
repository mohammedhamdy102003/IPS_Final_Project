using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPS_PROJECT.Data;
using IPS_PROJECT.Services;
using IPS_PROJECT.Models;
using System.Globalization;

namespace IPS_PROJECT.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PdfReportService _reportService;

        public ReportsController(AppDbContext context, PdfReportService reportService)
        {
            _context = context;
            _reportService = reportService;
        }

        [HttpGet("DownloadLogsPdf")]
        public async Task<IActionResult> DownloadLogsPdf(string range, string? start, string? end)
        {
            try
            {
                var query = _context.Events.AsNoTracking();

                DateTime? startDate = null;
                DateTime? endDate = null;


                string filterLabel = range?.ToLower() ?? "all";

                switch (filterLabel)
                {
                    case "today":
                        startDate = DateTime.Today;
                        endDate = DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59);
                        query = query.Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate);
                        break;

                    case "week":
                        startDate = DateTime.Today.AddDays(-7);
                        endDate = DateTime.Now;
                        query = query.Where(e => e.Timestamp >= startDate);
                        break;

                    case "month":
                        startDate = DateTime.Today.AddMonths(-1);
                        endDate = DateTime.Now;
                        query = query.Where(e => e.Timestamp >= startDate);
                        break;

                    case "custom":
                        if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
                        {
                            if (DateTime.TryParse(start, out var s) && DateTime.TryParse(end, out var e))
                            {
                                startDate = s.Date;
                                endDate = e.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                                query = query.Where(ev => ev.Timestamp >= startDate && ev.Timestamp <= endDate);
                            }
                        }
                        break;

                    default:

                        var eventsDefault = await _context.Events
                            .AsNoTracking()
                            .OrderByDescending(x => x.Timestamp)
                            .Take(100)
                            .ToListAsync();


                        return await GenerateFileResponse(eventsDefault, filterLabel, null, null);
                }

                var events = await query.OrderByDescending(x => x.Timestamp).ToListAsync();

                if (events == null || !events.Any())
                {
                    return Content($"No events found for the selected period: {range}");
                }


                return await GenerateFileResponse(events, filterLabel, startDate, endDate);
            }
            catch (Exception ex)
            {
                return BadRequest($"Internal Error: {ex.Message}");
            }
        }

        private async Task<IActionResult> GenerateFileResponse(List<EVENTS> events, string range, DateTime? start, DateTime? end)
        {
            int threats = events.Count(x => x.Status == "Blocked" || x.Prediction != "Benign");
            int benign = events.Count(x => x.Status == "Allowed" || x.Prediction == "Benign");


            byte[] pdfData = _reportService.GenerateExecutiveReport(events, threats, benign, range, start, end);

            string fileName = $"IPS_Report_{range}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfData, "application/pdf", fileName);
        }
    }
}