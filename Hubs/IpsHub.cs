using Microsoft.AspNetCore.SignalR;
using IPS_PROJECT.Models;

namespace IPS_PROJECT.Hubs
{
    public class IpsHub : Hub
    {
        // 
        public async Task SendAttackAlert(AlertNotification notification)
        {
            await Clients.All.SendAsync("ReceiveAttackAlert", notification);
        }

        // For Refrishing 
        public async Task RequestRefresh()
        {
            await Clients.All.SendAsync("ReceiveRefresh");
        }
    }
}