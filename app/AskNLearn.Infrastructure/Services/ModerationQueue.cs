using AskNLearn.Application.Common.Interfaces;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AskNLearn.Infrastructure.Services
{
    public class ModerationQueue : IModerationQueue
    {
        private readonly Channel<ModerationTask> _queue;

        public ModerationQueue()
        {
            // Unbounded channel, though in production you might want to bound it.
            _queue = Channel.CreateUnbounded<ModerationTask>();
        }

        public void Enqueue(ModerationTask task)
        {
            _queue.Writer.TryWrite(task);
        }

        public async Task<ModerationTask> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
