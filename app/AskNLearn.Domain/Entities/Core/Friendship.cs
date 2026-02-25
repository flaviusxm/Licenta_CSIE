using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Domain.Entities.Core
{
    public enum FriendshipStatus
    {
        Pending,
        Accepted,
        Blocked
    }
    [PrimaryKey(nameof(RequesterId), nameof(AddresseeId))]
    [Table("Friendships")]
    public class Friendship
    {
        public string RequesterId { get; set; } = null!;

        [ForeignKey(nameof(RequesterId))]
        public ApplicationUser Requester { get; set; } = null!;

        public string AddresseeId { get; set; } = null!; 

        [ForeignKey(nameof(AddresseeId))]
        public ApplicationUser Addressee { get; set; } = null!;

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
