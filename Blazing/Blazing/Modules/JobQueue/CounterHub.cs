using Microsoft.AspNetCore.SignalR;

namespace Blazing.Modules.JobQueue;

public class CounterHub : Hub
{
    public async Task SendRows(long rows)
    {
        await Clients.All.SendAsync("ReceiveMessage", rows);
    }
}
