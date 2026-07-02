namespace StudyTracker.Models.ViewModels
{
    public class WeeklyProgressViewModel
    {
        public DateOnly WeekStartDate { get; set; }
        public int DailyTargetMinutes { get; set; }
        public List<DayProgressViewModel> Days { get; set; } = new List<DayProgressViewModel>();
        public int AchievedDaysCount { get; set; }
        public double WeeklyCompletionPercentage { get; set; }
    }

    public class DayProgressViewModel
    {
        public DateOnly Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int StudiedMinutes { get; set; }
        public int TargetMinutes { get; set; }
        public bool IsAchieved => StudiedMinutes >= TargetMinutes;
        public double Hours => StudiedMinutes / 60.0;
        public double TargetHours => TargetMinutes / 60.0;
    }
}
