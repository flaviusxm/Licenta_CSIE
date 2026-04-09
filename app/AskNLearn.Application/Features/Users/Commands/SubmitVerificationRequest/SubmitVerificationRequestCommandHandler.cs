using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

            var verificationRequest = new VerificationRequest
            {
                UserId = request.UserId,
                StudentIdUrl = request.StudentIdUrl,
                CarnetUrl = request.CarnetUrl,
                Status = Status.Pending,
                SubmittedAt = System.DateTime.UtcNow,
                AdminNotes = $"[Guardian Analysis]: {recommendation} | Details: {details}"
            };

            _context.VerificationRequests.Add(verificationRequest);
            await _context.SaveChangesAsync(cancellationToken);

            return errors;
        }
    }
}
