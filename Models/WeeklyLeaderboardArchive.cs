using System.ComponentModel.DataAnnotations;

namespace StudyTracker.Models
{
    public class WeeklyLeaderboardArchive
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateOnly WeekStartDate { get; set; }
        public DateOnly WeekEndDate { get; set; }

        [Required]
        public string SnapshotJson { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

