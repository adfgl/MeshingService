namespace TriUgla.WebApi.Services
{
    public interface IJobStore
    {
        ValueTask<JobRecord?> GetAsync(string jobId, CancellationToken ct);
        ValueTask PutAsync(JobRecord job, CancellationToken ct);
        ValueTask<bool> TryUpdateAsync(string jobId, Func<JobRecord, bool> mutator, CancellationToken ct);
        IAsyncEnumerable<JobRecord> EnumerateAsync(CancellationToken ct);
    }
}
