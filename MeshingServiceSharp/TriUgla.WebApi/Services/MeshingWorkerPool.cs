namespace TriUgla.WebApi.Services
{
    public sealed class MeshingWorkerPool : BackgroundService
    {
        private readonly int _workers;
        private readonly IServiceProvider _sp;

        public MeshingWorkerPool(IServiceProvider sp, IConfiguration cfg)
        {
            _sp = sp;
            _workers = Math.Max(1, cfg.GetValue("Meshing:Workers", 2));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = new Task[_workers];
            for (int i = 0; i < _workers; i++)
                tasks[i] = RunOneAsync(stoppingToken);

            return Task.WhenAll(tasks);
        }

        private async Task RunOneAsync(CancellationToken ct)
        {
            using var scope = _sp.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<MeshingWorker>();
            await worker.StartAsync(ct); // not ideal

            // Better: refactor MeshingWorker into a reusable "ProcessLoop" service.
            // Keeping it short here; tell me if you want the clean version.
        }
    }

}
