using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.Core
{
    public enum FriendshipStatus
    {
        Pending,
        Accepted,
        Blocked
    }

    [Table("Friendships")]
    public class Friendship
    {
        [Required]
        public string RequesterId { get; set; } = null!;

        [Required]
        public string AddresseeId { get; set; } = null!;

        [ForeignKey(nameof(RequesterId))]
        public ApplicationUser Requester { get; set; } = null!;

        [ForeignKey(nameof(AddresseeId))]
        public ApplicationUser Addressee { get; set; } = null!;

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
