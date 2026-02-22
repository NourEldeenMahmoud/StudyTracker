using System.ComponentModel.DataAnnotations;

namespace StudyTracker.Models.ViewModels
{
    public class WeeklyTargetViewModel
    {
        [Required(ErrorMessage = "Daily target hours is required")]
        [Range(0.1, 16, ErrorMessage = "Daily target must be between 0.1 and 16 hours")]
        [Display(Name = "Daily Target Hours")]
        public double DailyTargetHours { get; set; }

        // WeekStartDate is calculated automatically (Saturday to Saturday)
        // Not shown in the form, but required for the service
        public DateOnly WeekStartDate { get; set; }

        public int DailyTargetMinutes => (int)(DailyTargetHours * 60);
    }
}
