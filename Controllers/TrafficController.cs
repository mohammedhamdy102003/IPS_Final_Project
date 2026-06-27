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
        private readonly IConfiguration _configuration;
        private readonly BatchBuilderService _batchBuilder;

        public TrafficController(AppDbContext context, AiPredictionService aiService, IHubContext<IpsHub> hubContext, IConfiguration configuration, BatchBuilderService batchBuilder)
        {
            _context = context;
            _aiService = aiService;
            _hubContext = hubContext;
            _configuration = configuration;
            _batchBuilder = batchBuilder;
        }

        [HttpPost("ProcessTraffic")]
        public async Task<IActionResult> ProcessTraffic([FromBody] TrafficIncomingRequest incoming)
        {
            try
            {
                if (incoming == null || incoming.data == null)
                    return BadRequest(new { error = "missing data" });

                var cleanedData = new Dictionary<string, object>();
                foreach (var item in incoming.data)
                {
                    if (item.Value is JsonElement element)
                    {
                        if (element.ValueKind == JsonValueKind.True) cleanedData[item.Key] = 1.0;
                        else if (element.ValueKind == JsonValueKind.False) cleanedData[item.Key] = 0.0;
                        else if (element.ValueKind == JsonValueKind.Number) cleanedData[item.Key] = element.GetDouble();
                        else if (element.ValueKind == JsonValueKind.String) cleanedData[item.Key] = element.GetString() ?? "";
                    }
                }

                if (!cleanedData.ContainsKey("protocol"))
                    cleanedData["protocol"] = (double)incoming.protocol;

                cleanedData["script_payload"] = incoming.script_payload ?? "";

                var rawResult = await _aiService.GetRawPredictionAsync(cleanedData);

                using var doc = JsonDocument.Parse(rawResult);
                var root = doc.RootElement;

                JsonElement result;
                if (root.TryGetProperty("results", out var resultsArray) && resultsArray.GetArrayLength() > 0)
                    result = resultsArray[0];
                else if (root.ValueKind == JsonValueKind.Array)
                    result = root[0];
                else
                    result = root;

                if (result.TryGetProperty("error", out var errorProp))
                    return BadRequest(new { error = errorProp.GetString() });

                bool isAnomaly = result.GetProperty("is_anomaly").GetBoolean();
                string Model_Attack_Type = result.GetProperty("predicted_class").GetString() ?? "Unknown";
                double confidenceValue = result.GetProperty("class_confidence").GetDouble();

                // تم تعديل منطق المقارنة ليتوافق مع الـ Confidence من الموديل الجديد
                string Attack_Type;
                if (!isAnomaly)
                {
                    Attack_Type = "Benign";
                }
                else if (confidenceValue > 0.5) 
                {
                    Attack_Type = Model_Attack_Type;
                }
                else
                {
                    Attack_Type = "Unknown Attack";
                }

                var trafficEvent = new EVENTS
                {
                    SourceIp = incoming.source_ip ?? "Unknown",
                    DestinationIp = incoming.destination_ip ?? "Unknown",
                    Prediction = isAnomaly ? "Anomaly" : "Non Anomaly",
                    AttackType = Attack_Type,
                    Confidence = confidenceValue * 100, 
                    Status = isAnomaly ? "Blocked" : "Allowed",
                    Timestamp = DateTime.Now
                };

                _context.Events.Add(trafficEvent);
                await _context.SaveChangesAsync();

                var totalEvents = await _context.Events.CountAsync();
                if (totalEvents % 20 == 0)
                {
                    var last20Events = await _context.Events
                            .OrderByDescending(x => x.Id)
                            .Take(20)
                            .ToListAsync();

                    var batch = _batchBuilder.BuildBatch(last20Events);

                    if (batch.Status == "Blocked")
                    {
                        var notification = new AlertNotification
                        {
                            AttackType = batch.AttackType,
                            SourceIp = batch.SourceIp,
                            DestinationIp = batch.DestinationIp,
                            Prediction =  batch.Prediction,
                            Confidence =  batch.Confidence,
                            Timestamp = batch.Timestamp,
                            IsRead = false
                        };
                        _context.AlertNotifications.Add(notification);
                        await _context.SaveChangesAsync();
                        await _hubContext.Clients.All.SendAsync("ReceiveAttackAlert", notification);
                    }
                    else
                    {
                        await _context.SaveChangesAsync();
                    }
                    await _hubContext.Clients.All.SendAsync("ReceiveNewBatch", batch);
                }

                return Ok(new { source_ip = trafficEvent.SourceIp, destination_ip = trafficEvent.DestinationIp, attack_type = Attack_Type, confidence = confidenceValue, status = trafficEvent.Status });
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
                builder.AppendLine("Source IP,Destination IP,Prediction, Attack_Type,Confidence,Status,Timestamp");
                foreach (var e in events)
                {
                    builder.AppendLine($"{e.SourceIp},{e.DestinationIp},{e.Prediction},{e.AttackType},{e.Confidence}%,{e.Status},{e.Timestamp}");
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
                var unreadAlerts = await _context.AlertNotifications.Where(n => !n.IsRead).ToListAsync();
                foreach (var alert in unreadAlerts) { alert.IsRead = true; }
                await _context.SaveChangesAsync();
                return Ok(new { message = "All alerts marked as read" });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
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