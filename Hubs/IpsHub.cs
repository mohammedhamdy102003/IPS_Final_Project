using IPS_PROJECT.Data;
using IPS_PROJECT.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IPS_PROJECT.Hubs
{
    public class IpsHub : Hub
    {
        // 

        private readonly AppDbContext _context;

        public IpsHub(AppDbContext context)
        {
            _context = context;
        }
        public async Task SendAttackAlert(AlertNotification notification)
        {
            await Clients.All.SendAsync("ReceiveAttackAlert", notification);
        }


        // For Refrishing 
        public async Task RequestRefresh()
        {
            await Clients.All.SendAsync("ReceiveRefresh");
        }

        /*
        // for updating the dashboard stats 
        public async Task SendDashboardStats()
        {
            var totalEvents = await _context.Events.CountAsync();
            var threatsBlocked = await _context.Events.CountAsync(x => x.Status == "Blocked");
            var benignTraffic = await _context.Events.CountAsync(x => x.Status == "Allowed");

            await Clients.All.SendAsync("UpdateDashboard", new
            {
                totalEvents,
                threatsBlocked,
                benignTraffic
            });
        }*/
    }
}