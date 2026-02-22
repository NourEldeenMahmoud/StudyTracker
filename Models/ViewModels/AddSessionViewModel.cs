using System.ComponentModel.DataAnnotations;

namespace StudyTracker.Models.ViewModels
{
    public class AddSessionViewModel
    {
        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        [Required(ErrorMessage = "Hours is required")]
        [Range(0, 16, ErrorMessage = "Hours must be between 0 and 16")]
        [Display(Name = "Hours")]
        public int Hours { get; set; } = 0;

        [Required(ErrorMessage = "Minutes is required")]
        [Range(0, 59, ErrorMessage = "Minutes must be between 0 and 59")]
        [Display(Name = "Minutes")]
        public int Minutes { get; set; } = 0;

        [MaxLength(250, ErrorMessage = "Notes cannot exceed 250 characters")]
        [Display(Name = "Notes (Optional)")]
        public string? Notes { get; set; }

        public int DurationMinutes => Hours * 60 + Minutes;
    }
}
