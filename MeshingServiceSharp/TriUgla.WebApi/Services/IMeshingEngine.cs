namespace TriUgla.WebApi.Services
{
    public interface IMeshingEngine
    {
        Task<MeshingResult> MeshAsync(
            string jobId,
            MeshingRequest request,
            IProgress<int> progress,
            CancellationToken ct);
    }

    public sealed class DummyMeshingEngine : IMeshingEngine
    {
        public async Task<MeshingResult> MeshAsync(string jobId, MeshingRequest request, IProgress<int> progress, CancellationToken ct)
        {
            for (int p = 0; p <= 100; p += 10)
            {
                ct.ThrowIfCancellationRequested();
                progress.Report(p);
                await Task.Delay(50, ct);
            }

            return new MeshingResult(
                jobId,
                "json",
                "{ \"vertices\": [], \"triangles\": [] }");
        }
    }
}
