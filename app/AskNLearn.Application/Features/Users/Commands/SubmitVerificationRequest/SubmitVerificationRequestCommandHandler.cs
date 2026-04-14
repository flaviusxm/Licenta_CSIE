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
        private readonly IGuardianClient _guardianClient;

        public SubmitVerificationRequestCommandHandler(IApplicationDbContext context, IGuardianClient guardianClient)
        {
            _context = context;
            _guardianClient = guardianClient;
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

            var (isValid, details, recommendation) = await _guardianClient.VerifyDocumentAsync(null, request.StudentIdUrl);

            // Autonomous Moderation: Auto-approve if AI is 100% confident
            bool autoApproved = isValid && recommendation.Contains("Approved", StringComparison.OrdinalIgnoreCase);

            var verificationRequest = new VerificationRequest
            {
                UserId = request.UserId,
                StudentIdUrl = request.StudentIdUrl,
                CarnetUrl = request.CarnetUrl,
                Status = autoApproved ? Status.Approved : Status.Pending,
                SubmittedAt = DateTime.UtcNow,
                AdminNotes = $"[Guardian Analysis]: {recommendation} | Details: {details}",
                ProcessedAt = autoApproved ? DateTime.UtcNow : null,
                ProcessedBy = autoApproved ? "SYSTEM_AI" : null
            };

            if (autoApproved)
            {
                var user = await _context.Users.FindAsync(request.UserId);
                if (user != null)
                {
                    user.IsVerified = true;
                    user.VerificationStatus = UserVerificationStatus.IdentityVerified;
                }
            }

            _context.VerificationRequests.Add(verificationRequest);
            await _context.SaveChangesAsync(cancellationToken);

            return errors;
        }
    }
}
