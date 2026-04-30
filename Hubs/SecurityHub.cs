using Microsoft.AspNetCore.SignalR;
    
    namespace IPS_PROJECT.Hubs
{
    public class SecurityHub : Hub 
    {
        public async Task SendSecurityUpdate(bool isSecure)
        {
            await Clients.All.SendAsync("ReceiveSecurityUpdate", isSecure);
        }
    }
}
