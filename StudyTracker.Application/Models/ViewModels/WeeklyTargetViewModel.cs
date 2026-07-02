using System.ComponentModel.DataAnnotations;

namespace StudyTracker.Models.ViewModels
{
    public class WeeklyTargetViewModel : IValidatableObject
    {
        [Range(0, 23, ErrorMessage = "Hours must be between 0 and 23")]
        public int Hours { get; set; }

        [Range(0, 59, ErrorMessage = "Minutes must be between 0 and 59")]
        public int Minutes { get; set; }

        public DateOnly WeekStartDate { get; set; }

        public int DailyTargetMinutes => Hours * 60 + Minutes;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Hours == 0 && Minutes == 0)
                yield return new ValidationResult("Daily target must be at least 1 minute.", new[] { nameof(Hours) });

            if (Hours * 60 + Minutes > 23 * 60 + 59)
                yield return new ValidationResult("Daily target cannot exceed 23 hours 59 minutes.", new[] { nameof(Hours) });
        }
    }
}
