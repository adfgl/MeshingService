using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TriUgla.WebApi.Services;

namespace TriUgla.WebApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/meshing/jobs")]
    public sealed class MeshingController : ControllerBase
    {
        private readonly IJobStore _store;
        private readonly IJobQueue _queue;

        public MeshingController(IJobStore store, IJobQueue queue)
        {
            _store = store;
            _queue = queue;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        public sealed record MeshingRequest(
            string Format,
            string Payload);

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] MeshingRequest req, CancellationToken ct)
        {
            var jobId = Guid.NewGuid().ToString("n");

            var job = new JobRecord
            {
                JobId = jobId,
                UserId = UserId,
                Request = req,
                Status = JobStatus.Queued,
                Progress = 0
            };

            await _store.PutAsync(job, ct);
            await _queue.EnqueueAsync(new MeshingWorkItem(jobId), ct);

            return AcceptedAtAction(nameof(GetStatus), new { jobId }, new { jobId });
        }

        public sealed class MeshingJobResponse(
              string JobId,
              JobStatus Status,
              int Progress,
              string? Error,
              DateTimeOffset CreatedAt,
              DateTimeOffset? StartedAt,
              DateTimeOffset? FinishedAt);

        [HttpGet("{jobId}")]
        public async Task<ActionResult<MeshingJobResponse>> GetStatus([FromRoute] string jobId, CancellationToken ct)
        {
            var job = await _store.GetAsync(jobId, ct);
            if (job is null || job.UserId != UserId) return NotFound();

            return new MeshingJobResponse(
                job.JobId, job.Status, job.Progress, job.Error,
                job.CreatedAt, job.StartedAt, job.FinishedAt);
        }

        [HttpGet("{jobId}/result")]
        public async Task<IActionResult> GetResult([FromRoute] string jobId, CancellationToken ct)
        {
            var job = await _store.GetAsync(jobId, ct);
            if (job is null || job.UserId != UserId) return NotFound();

            return job.Status switch
            {
                JobStatus.Finished when job.Result is not null => Ok(job.Result),
                JobStatus.Failed => Problem(job.Error ?? "Failed."),
                JobStatus.Canceled => Conflict(new { message = "Canceled." }),
                _ => Accepted(new { message = "Not ready.", job.Status, job.Progress })
            };
        }

        [HttpDelete("{jobId}")]
        public async Task<IActionResult> Cancel([FromRoute] string jobId, CancellationToken ct)
        {
            var ok = await _store.TryUpdateAsync(jobId, job =>
            {
                if (job.UserId != UserId) return false;

                if (job.Status is JobStatus.Finished or JobStatus.Failed or JobStatus.Canceled)
                    return true;

                job.Status = JobStatus.Canceled;
                job.FinishedAt = DateTimeOffset.UtcNow;
                job.Error = null;
                job.Cts.Cancel();
                return true;
            }, ct);

            return ok ? NoContent() : NotFound();
        }
    }
}
