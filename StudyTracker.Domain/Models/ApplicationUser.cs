using Microsoft.AspNetCore.Identity;

namespace StudyTracker.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsSuspended { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ProfilePictureUrl { get; set; }
        
        // Navigation properties
        public ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();
        public ICollection<UserWeeklyTarget> WeeklyTargets { get; set; } = new List<UserWeeklyTarget>();
    }
}
