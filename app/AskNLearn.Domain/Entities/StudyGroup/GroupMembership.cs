using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Domain.Entities.StudyGroup
{
   
        [PrimaryKey(nameof(GroupId), nameof(UserId))]
        [Table("GroupMemberships")]
        public class GroupMembership
        {
            public Guid GroupId { get; set; } 
            public string UserId { get; set; } 
            public Guid GroupRoleId { get; set; } 
            public DateTime JoinedAt { get; set; }
            public bool IsBanned { get; set; } = false;
            
            [ForeignKey(nameof(GroupId))]
            public StudyGroup Group { get; set; } = null!;

            [ForeignKey(nameof(UserId))]
            public ApplicationUser User { get; set; } = null!;

            [ForeignKey(nameof(GroupRoleId))]
            public GroupRole Role { get; set; } = null!;
        }
    }

