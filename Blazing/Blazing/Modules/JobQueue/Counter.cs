using Microsoft.AspNetCore.SignalR;

namespace Blazing.Modules.JobQueue;

public interface IRowCounter
{
    long Value { get; }
    long Increment();
    long Finish();

    event Func<long, Task> OnChangedAsync;
    event Func<long, Task> OnFinishedAsync;
}

public class RowCounter : IRowCounter, IDisposable
{
    private readonly IHubContext<CounterHub> _hubContext;
    private long _value;

    public event Func<long, Task>? OnChangedAsync;
    public event Func<long, Task>? OnFinishedAsync;

    public RowCounter(IHubContext<CounterHub> hubContext)
    {
        _hubContext = hubContext;
        OnChangedAsync += ChangedMessage;
        OnFinishedAsync += FinishedMessage;
    }

    public async Task ChangedMessage(long rows)
    {
        if (_value % 100_000 == 0)
        {
            await _hubContext
                .Clients
                .All
                .SendAsync("ReceiveMessage", rows)
                .ConfigureAwait(false);
        }
    }
    
    public async Task FinishedMessage(long rows)
    {
        await _hubContext
            .Clients
            .All
            .SendAsync("ReceiveMessage", rows)
            .ConfigureAwait(false);
    }
    
    public long Value =>
        _value;

    public long Increment()
    {
        var newValue = Interlocked.Increment(ref _value);
        OnChangedAsync?.Invoke(newValue);
        return newValue;
    }

    public long Finish()
    {
        var newValue = _value;
        OnFinishedAsync?.Invoke(newValue);
        return newValue;
    }

    public void Dispose()
    {
        OnChangedAsync -= ChangedMessage;
        OnFinishedAsync -= FinishedMessage;
    }

}