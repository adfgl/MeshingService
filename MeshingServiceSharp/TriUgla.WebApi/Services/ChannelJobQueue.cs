using System.Threading.Channels;

namespace TriUgla.WebApi.Services
{
    public sealed class ChannelJobQueue : IJobQueue
    {
        private readonly Channel<MeshingWorkItem> _channel;

        public ChannelJobQueue(int capacity = 10_000)
        {
            _channel = Channel.CreateBounded<MeshingWorkItem>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
        }

        public ValueTask EnqueueAsync(MeshingWorkItem item, CancellationToken ct)
            => _channel.Writer.WriteAsync(item, ct);

        public ValueTask<MeshingWorkItem> DequeueAsync(CancellationToken ct)
            => _channel.Reader.ReadAsync(ct);
    }
}
