using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using IPS_PROJECT.Services;
using IPS_PROJECT.Models;
using IPS_PROJECT.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using IPS_PROJECT.Hubs;

namespace IPS_PROJECT.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrafficController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AiPredictionService _aiService;
        private readonly IHubContext<IpsHub> _hubContext;
        private readonly IConfiguration _configuration; // أضفنا هذا السطر

        public TrafficController(AppDbContext context, AiPredictionService aiService, IHubContext<IpsHub> hubContext, IConfiguration configuration)
        {
            _context = context;
            _aiService = aiService;
            _hubContext = hubContext;
            _configuration = configuration; // أضفنا هذا السطر
        }

        [HttpPost("ProcessTraffic")]
        public async Task<IActionResult> ProcessTraffic([FromBody] TrafficIncomingRequest incoming)
        {
            try
            {
                if (incoming == null || incoming.data == null)
                    return BadRequest(new { error = "missing data" });

                var cleanedData = new Dictionary<string, double>();
                foreach (var item in incoming.data)
                {
                    if (item.Value is JsonElement element)
                    {
                        if (element.ValueKind == JsonValueKind.True) cleanedData[item.Key] = 1.0;
                        else if (element.ValueKind == JsonValueKind.False) cleanedData[item.Key] = 0.0;
                        else if (element.ValueKind == JsonValueKind.Number) cleanedData[item.Key] = element.GetDouble();
                    }
                }

                if (!cleanedData.ContainsKey("protocol"))
                    cleanedData["protocol"] = (double)incoming.protocol;

                var rawResult = await _aiService.GetRawPredictionAsync(cleanedData);
                using var doc = JsonDocument.Parse(rawResult);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var errorProp))
                    return BadRequest(new { error = errorProp.GetString() });

                bool isAnomaly = root.GetProperty("anomaly_head").GetProperty("is_anomaly").GetBoolean();

                string prediction = root.GetProperty("classification_head").GetProperty("predicted_class").GetString() ?? "Unknown";
                double confidenceRaw = root.GetProperty("classification_head").GetProperty("confidence").GetDouble();
                double confidenceValue = Math.Round(confidenceRaw * 100, 2);

                var trafficEvent = new EVENTS
                {
                    SourceIp = incoming.source_ip ?? "Unknown",
                    DestinationIp = incoming.destination_ip ?? "Unknown",
                    TrafficType = incoming.protocol == 6 ? "TCP" : "UDP",
                    Prediction = prediction,
                    Confidence = confidenceValue,
                    Status = isAnomaly ? "Blocked" : "Allowed",
                    Timestamp = DateTime.Now
                };

                _context.Events.Add(trafficEvent);

                if (trafficEvent.Status == "Blocked")
                {
                    var notification = new AlertNotification
                    {
                        AttackType = prediction,
                        SourceIp = trafficEvent.SourceIp,
                        DestinationIp = trafficEvent.DestinationIp,
                        Prediction = prediction,
                        Confidence = confidenceValue,
                        Protocol = trafficEvent.TrafficType,
                        Timestamp = trafficEvent.Timestamp,
                        IsRead = false
                    };
                    _context.AlertNotifications.Add(notification);
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("ReceiveAttackAlert", notification);
                }
                else
                {
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                }

                return Ok(new
                {
                    source_ip = trafficEvent.SourceIp,
                    destination_ip = trafficEvent.DestinationIp,
                    attack_type = prediction,
                    confidence = confidenceValue, // تم تعديل confString إلى confidenceValue
                    status = trafficEvent.Status
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("ExportTraffic")]
        public async Task<IActionResult> ExportTraffic()
        {
            try
            {
                var events = await _context.Events.OrderByDescending(e => e.Timestamp).ToListAsync();
                var builder = new StringBuilder();
                builder.AppendLine("Source IP,Destination IP,Protocol,Prediction,Confidence,Status,Timestamp");

                foreach (var e in events)
                {
                    builder.AppendLine($"{e.SourceIp},{e.DestinationIp},{e.TrafficType},{e.Prediction},{e.Confidence}%,{e.Status},{e.Timestamp}");
                }

                return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"Traffic_Report_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("GetNotificationDetails/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var alert = await _context.AlertNotifications.FindAsync(id);
            if (alert == null) return NotFound();
            return Ok(alert);
        }

        [HttpPost("MarkAsRead")]
        public async Task<IActionResult> MarkAsRead()
        {
            try
            {
                var unreadAlerts = await _context.AlertNotifications
                                                 .Where(n => !n.IsRead)
                                                 .ToListAsync();

                foreach (var alert in unreadAlerts)
                {
                    alert.IsRead = true;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "All alerts marked as read" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("DeleteNotification/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var alert = await _context.AlertNotifications.FindAsync(id);
            if (alert == null) return NotFound();

            _context.AlertNotifications.Remove(alert);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("ClearAllNotifications")]
        public async Task<IActionResult> ClearAll()
        {
            var all = await _context.AlertNotifications.ToListAsync();
            _context.AlertNotifications.RemoveRange(all);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}