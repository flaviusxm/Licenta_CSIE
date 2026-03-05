using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Communities.Commands.DeleteCommunity
{
    public class DeleteCommunityCommandHandler : IRequestHandler<DeleteCommunityCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileService _fileService;

        public DeleteCommunityCommandHandler(IApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<bool> Handle(DeleteCommunityCommand request, CancellationToken cancellationToken)
        {
            var community = await _context.Communities
                .Include(c => c.Posts)
                    .ThenInclude(p => p.Attachments)
                .Include(c => c.Posts)
                    .ThenInclude(p => p.Votes)
                .Include(c => c.Posts)
                    .ThenInclude(p => p.UniqueViews)
                .Include(c => c.Posts)
                    .ThenInclude(p => p.Comments)
                        .ThenInclude(m => m.Attachments)
                            .ThenInclude(a => a.File)
                .Include(c => c.Posts)
                    .ThenInclude(p => p.Comments)
                        .ThenInclude(m => m.Reactions)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
            
            if (community == null) return false;

            // Delete community image if exists
            if (!string.IsNullOrEmpty(community.ImageUrl))
            {
                _fileService.DeleteFile(community.ImageUrl);
            }

            // Remove memberships
            var memberships = await _context.CommunityMemberships
                .Where(m => m.CommunityId == community.Id)
                .ToListAsync(cancellationToken);
            _context.CommunityMemberships.RemoveRange(memberships);

            // Cleanup posts and their children
            foreach (var post in community.Posts)
            {
                // Delete post attachments
                foreach (var attachment in post.Attachments)
                {
                    if (!string.IsNullOrEmpty(attachment.Url))
                    {
                        _fileService.DeleteFile(attachment.Url);
                    }
                    _context.PostAttachments.Remove(attachment);
                }

                // Delete post comments and their attachments/reactions
                foreach (var comment in post.Comments)
                {
                    foreach (var attachment in comment.Attachments)
                    {
                        if (attachment.File != null)
                        {
                            _fileService.DeleteFile(attachment.File.FilePath);
                            _context.StoredFiles.Remove(attachment.File);
                        }
                        _context.MessageAttachments.Remove(attachment);
                    }

                    _context.MessageReactions.RemoveRange(comment.Reactions);
                    _context.Messages.Remove(comment);
                }

                _context.PostVotes.RemoveRange(post.Votes);
                _context.PostViews.RemoveRange(post.UniqueViews);
                _context.Posts.Remove(post);
            }

            _context.Communities.Remove(community);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
