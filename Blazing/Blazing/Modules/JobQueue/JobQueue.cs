using System.Collections.Concurrent;
using System.Diagnostics;
using Dapper;
using Npgsql;

namespace Blazing.Modules.JobQueue;

public interface IJobQueue
{
    void Submit(string line);
    void Shutdown();
}

public class JobQueue(NpgsqlDataSource connPool, IRowCounter rows, ILogger<JobQueue> logger) : IJobQueue
{
    private readonly object _startQueueMutex = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task[] _workerTasks = [];
    private BlockingCollection<string>? _lazyQueue;

    private int _concurrentWorkerThreads;
    private DateTime _now;
    private string _version;
    private string _timestamp;

    public void Submit(string line)
    {
        var perProducerSubmitToken = _cancellationTokenSource.Token;
        Queue().Add(line, perProducerSubmitToken);
    }

    private BlockingCollection<string> Queue()
    {
        lock (_startQueueMutex)
        {
            if (_lazyQueue == null)
            {
                _concurrentWorkerThreads = 1;
                _lazyQueue = new BlockingCollection<string>(_concurrentWorkerThreads);
                _workerTasks = new Task[_concurrentWorkerThreads];
                StartBackgroundWorkers();

                _now = DateTime.UtcNow;
                Stopwatch.StartNew();
                _version = _now.Ticks + ",";
                _timestamp = _now.ToString("O") + ",";
            }

            return _lazyQueue;
        }
    }

    private void StartBackgroundWorkers()
    {
        for (var i = 0; i < _workerTasks.Length; i++)
        {
            var taskNumber = i;
            _workerTasks[i] = Task.Run(() => ActionLoop(taskNumber));
        }
    }

    private void ActionLoop(int taskNumber)
    {
        var sql = """
                  copy export_import_experiment (
                      version,
                      timestamp,
                      ssoid,
                      account_number,
                      product_name
                  ) from stdin with delimiter ',' csv;
                  """;

        var perConsumerTaskToken = _cancellationTokenSource.Token;
        using var conn = connPool.OpenConnection();
        conn.Execute("""
                     set idle_in_transaction_session_timeout = 0;
                     set idle_session_timeout = 0;
                     set lock_timeout = 0;
                     set statement_timeout = 0;
                     set tcp_keepalives_idle = 10;
                     set tcp_keepalives_interval = 5;
                     """);
        using var writer = conn.BeginTextImport(sql);
        while (!Queue().IsCompleted && !perConsumerTaskToken.IsCancellationRequested)
        {
            try
            {
                if (Queue().TryTake(out var line, 1000, perConsumerTaskToken))
                {
                    writer.Write(_version);
                    writer.Write(_timestamp);
                    writer.WriteLine(line);
                }
            }
            catch (Exception e) when (e is ObjectDisposedException or OperationCanceledException)
            {
                logger.LogInformation("JobQueue.Worker: task {I} exiting normally on request", taskNumber);
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "JobQueue.Worker: task {I} dismissing unhandled exception from action", taskNumber);
            }
        }

        Interlocked.Decrement(ref _concurrentWorkerThreads);
        logger.LogInformation("JobQueue.Worker: queue size: {QueueSize} - task rows: {Rows} - task {I} exiting", 
            Queue().Count, rows.Value, taskNumber);
    }

    public Task ShutdownAsync()
    {
        return Task.Run(Shutdown);
    }

    public void Shutdown()
    {
        logger.LogInformation($"JobQueue.{nameof(Shutdown)}: Received SIGTERM. Stopping job queue");
        var shutdownReporterTask = Task.Run(async () =>
        {
            // Exits last after everything else exits
            while (Interlocked.CompareExchange(ref _concurrentWorkerThreads, 0, 0) > 0)
            {
                logger.LogInformation("JobQueue.Shutdown: waiting for workers to finish execution - {ConcurrentWorkerThreads} threads", _concurrentWorkerThreads);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            logger.LogInformation("JobQueue.Shutdown: waited for workers to finish execution - final {ConcurrentWorkerThreads} threads", _concurrentWorkerThreads);
        });

        Queue().CompleteAdding();
        rows.Finish();
        shutdownReporterTask.Wait(TimeSpan.FromSeconds(30));
        logger.LogInformation("JobQueue.Shutdown: shutting down");
        Queue().Dispose();
        _lazyQueue = null;
    }
}