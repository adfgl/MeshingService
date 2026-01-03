namespace TriUgla.WebApi.Services
{
    using System.Collections.Concurrent;

    public sealed class InMemoryJobStore : IJobStore
    {
        private readonly ConcurrentDictionary<string, JobRecord> _jobs = new(StringComparer.Ordinal);

        public ValueTask<JobRecord?> GetAsync(string jobId, CancellationToken ct)
            => ValueTask.FromResult(_jobs.TryGetValue(jobId, out var job) ? job : null);

        public ValueTask PutAsync(JobRecord job, CancellationToken ct)
        {
            _jobs[job.JobId] = job;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryUpdateAsync(string jobId, Func<JobRecord, bool> mutator, CancellationToken ct)
        {
            if (!_jobs.TryGetValue(jobId, out var job)) return ValueTask.FromResult(false);
            lock (job)
            {
                return ValueTask.FromResult(mutator(job));
            }
        }

        public async IAsyncEnumerable<JobRecord> EnumerateAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            foreach (var kv in _jobs)
            {
                ct.ThrowIfCancellationRequested();
                yield return kv.Value;
                await Task.Yield();
            }
        }
    }
}
