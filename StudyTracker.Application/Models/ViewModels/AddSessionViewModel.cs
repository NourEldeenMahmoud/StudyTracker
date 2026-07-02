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

        [Required(ErrorMessage = "Please describe what you studied.")]
        [MinLength(1, ErrorMessage = "Please describe what you studied.")]
        [MaxLength(250, ErrorMessage = "Notes cannot exceed 250 characters")]
        [Display(Name = "Notes")]
        public string Notes { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Source { get; set; } = "manual";

        /// <summary>Optional id from the focus timer client state; used to ignore duplicate POSTs for the same run.</summary>
        [MaxLength(64)]
        public string? TimerRunId { get; set; }

        public int DurationMinutes => Hours * 60 + Minutes;
    }
}
