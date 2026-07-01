using System.ComponentModel.DataAnnotations;

namespace StudyTracker.Models
{
    public class SecurityLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string TargetType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TargetId { get; set; }

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

