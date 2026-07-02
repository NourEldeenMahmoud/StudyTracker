namespace StudyTracker.Models.ViewModels
{
    public class WeeklyTargetProgressViewModel
    {
        public int DailyTargetMinutes { get; set; }
        public int TodayDoneMinutes { get; set; }
        public double ProgressPercentage => DailyTargetMinutes > 0 ? (TodayDoneMinutes * 100.0 / DailyTargetMinutes) : 0;
        public bool IsAchieved => TodayDoneMinutes >= DailyTargetMinutes;
        public int StreakDays { get; set; }
    }
}
