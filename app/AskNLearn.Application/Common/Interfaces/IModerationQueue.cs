using System;
using System.Threading;
using System.Threading.Tasks;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Application.Common.Interfaces
{
    public interface IModerationQueue
    {
        void Enqueue(ModerationTask task);
        Task<ModerationTask> DequeueAsync(CancellationToken cancellationToken);
    }

    public class ModerationTask
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = null!;
        public string? Title { get; set; }
        public ModerationTarget Target { get; set; }
        public ReportReason? Reason { get; set; }
    }

    public enum ModerationTarget
    {
        Post,
        Comment,
        Message,
        Report,
        IdentityVerification,
        Resource
    }
}
