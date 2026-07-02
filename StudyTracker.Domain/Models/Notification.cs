using System.ComponentModel.DataAnnotations;

namespace StudyTracker.Models
{
    public class Notification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(60)]
        public string? RelatedEntityType { get; set; }

        [MaxLength(120)]
        public string? RelatedEntityId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

