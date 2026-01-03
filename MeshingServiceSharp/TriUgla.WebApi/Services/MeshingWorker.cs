namespace TriUgla.WebApi.Services
{
    public sealed class MeshingWorker : BackgroundService
    {
        private readonly IJobQueue _queue;
        private readonly IJobStore _store;
        private readonly IMeshingEngine _engine;
        private readonly ILogger<MeshingWorker> _log;

        public MeshingWorker(IJobQueue queue, IJobStore store, IMeshingEngine engine, ILogger<MeshingWorker> log)
        {
            _queue = queue;
            _store = store;
            _engine = engine;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                MeshingWorkItem item;
                try { item = await _queue.DequeueAsync(stoppingToken); }
                catch (OperationCanceledException) { break; }

                var job = await _store.GetAsync(item.JobId, stoppingToken);
                if (job is null) continue;

                // Skip finished/canceled
                if (job.Status is JobStatus.Finished or JobStatus.Failed or JobStatus.Canceled)
                    continue;

                // Move Queued -> Running
                await _store.TryUpdateAsync(job.JobId, j =>
                {
                    if (j.Status != JobStatus.Queued) return false;
                    j.Status = JobStatus.Running;
                    j.StartedAt = DateTimeOffset.UtcNow;
                    j.Progress = 0;
                    j.Error = null;
                    return true;
                }, stoppingToken);

                try
                {
                    var progress = new Progress<int>(p =>
                    {
                        _ = _store.TryUpdateAsync(job.JobId, j =>
                        {
                            if (j.Status != JobStatus.Running) return false;
                            j.Progress = Math.Clamp(p, 0, 100);
                            return true;
                        }, CancellationToken.None);
                    });

                    var result = await _engine.MeshAsync(job.JobId, job.Request, progress, job.Cts.Token);

                    await _store.TryUpdateAsync(job.JobId, j =>
                    {
                        j.Result = result;
                        j.Status = JobStatus.Finished;
                        j.Progress = 100;
                        j.FinishedAt = DateTimeOffset.UtcNow;
                        return true;
                    }, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    await _store.TryUpdateAsync(job.JobId, j =>
                    {
                        j.Status = JobStatus.Canceled;
                        j.FinishedAt = DateTimeOffset.UtcNow;
                        j.Error = null;
                        return true;
                    }, stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Meshing job {JobId} failed", job.JobId);
                    await _store.TryUpdateAsync(job.JobId, j =>
                    {
                        j.Status = JobStatus.Failed;
                        j.FinishedAt = DateTimeOffset.UtcNow;
                        j.Error = ex.Message;
                        return true;
                    }, stoppingToken);
                }
            }
        }
    }

}
