namespace TriUgla.WebApi.Services
{
    public interface IJobQueue
    {
        ValueTask EnqueueAsync(MeshingWorkItem item, CancellationToken ct);
        ValueTask<MeshingWorkItem> DequeueAsync(CancellationToken ct);
    }
}
