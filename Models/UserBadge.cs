using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyTracker.Models
{
    public class UserBadge
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string BadgeKey { get; set; } = string.Empty;

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

        /// <summary>False until the user has seen the badge toast notification.</summary>
        public bool IsNotified { get; set; } = false;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;
    }
}
