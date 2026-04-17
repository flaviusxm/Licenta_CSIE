using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace AskNLearn.Application.Features.Users.Commands.SubmitVerificationRequest
{
    public class SubmitVerificationRequestCommandHandler : IRequestHandler<SubmitVerificationRequestCommand, List<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IModerationQueue _moderationQueue;

        public SubmitVerificationRequestCommandHandler(IApplicationDbContext context, IModerationQueue moderationQueue)
        {
            _context = context;
            _moderationQueue = moderationQueue;
        }

        public async Task<List<string>> Handle(SubmitVerificationRequestCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<string>();

            // Check if user already has a pending or approved request
            var existingRequest = await _context.VerificationRequests
                .FirstOrDefaultAsync(v => v.UserId == request.UserId && (v.Status == Status.Pending || v.Status == Status.Approved), cancellationToken);

            if (existingRequest != null)
            {
                if (existingRequest.Status == Status.Pending)
                    errors.Add("You already have a pending verification request.");
                else
                    errors.Add("Your account is already verified.");
                
                return errors;
            }

            var verificationRequest = new VerificationRequest
            {
                UserId = request.UserId,
                StudentIdUrl = request.StudentIdUrl,
                CarnetUrl = request.CarnetUrl,
                Status = Status.Pending,
                SubmittedAt = DateTime.UtcNow,
                AdminNotes = "[Guardian AI]: Analiză în curs...",
                ProcessedAt = null,
                ProcessedBy = null
            };

            _context.VerificationRequests.Add(verificationRequest);
            await _context.SaveChangesAsync(cancellationToken);

            // Enqueue for Guardian Shield (Background AI Processing)
            _moderationQueue.Enqueue(new ModerationTask
            {
                Id = verificationRequest.Id,
                Content = request.StudentIdUrl, // Primary image to verify
                Title = request.UserId, // Link to user
                Target = ModerationTarget.IdentityVerification
            });

            return errors;
        }
    }
}
