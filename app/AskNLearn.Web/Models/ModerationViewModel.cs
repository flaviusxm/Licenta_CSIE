using System.Collections.Generic;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.Messaging;

namespace AskNLearn.Web.Models
{
    public class ModerationViewModel
    {
        public IEnumerable<Post> FlaggedPosts { get; set; } = new List<Post>();
        public IEnumerable<Message> FlaggedMessages { get; set; } = new List<Message>();
        public IEnumerable<Report> UserReports { get; set; } = new List<Report>();
    }
}
