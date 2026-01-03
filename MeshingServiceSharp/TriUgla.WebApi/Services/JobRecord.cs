using static TriUgla.WebApi.Controllers.MeshingController;

namespace TriUgla.WebApi.Services
{
    public sealed class JobRecord
    {
        public required string JobId { get; init; }
        public required string UserId { get; init; }

        public required MeshingRequest Request { get; init; }

        public JobStatus Status { get; set; } = JobStatus.Queued;
        public int Progress { get; set; } = 0;
        public string? Error { get; set; }

        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }

        public MeshingResult? Result { get; set; }

        public CancellationTokenSource Cts { get; } = new();
    }
}
