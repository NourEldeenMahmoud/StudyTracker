using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyTracker.Models
{
    public class UserWeeklyTarget
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [DataType(DataType.Date)]
        public DateOnly WeekStartDate { get; set; }
        
        [Required]
        [Range(1, 960, ErrorMessage = "Daily target must be between 1 and 960 minutes (16 hours)")]
        public int DailyTargetMinutes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;
    }
}
