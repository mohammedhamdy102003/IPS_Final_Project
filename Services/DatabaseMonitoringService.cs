using IPS_PROJECT.Hubs;
using IPS_PROJECT.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IPS_PROJECT.Services;

public class DatabaseMonitoringService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<IpsHub> _hubContext;
    private int _lastEventCount = -1;

    public DatabaseMonitoringService(IServiceScopeFactory scopeFactory, IHubContext<IpsHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var currentCount = await context.Events.CountAsync(stoppingToken);

                    if (_lastEventCount == -1)
                    {
                        _lastEventCount = currentCount;
                    }
                    else if (currentCount > _lastEventCount)
                    {
                        _lastEventCount = currentCount;
                        
                        await _hubContext.Clients.All.SendAsync("ReceiveRefresh", stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking database: {ex.Message}");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}