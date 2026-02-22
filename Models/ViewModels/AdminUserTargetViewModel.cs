namespace StudyTracker.Models.ViewModels
{
    public class AdminUserTargetViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? DailyTargetMinutes { get; set; }
        public DateOnly? WeekStartDate { get; set; }
        public int? WeeklyTotalMinutes { get; set; }
        public int? WeeklyTargetMinutes { get; set; }
        public double? WeeklyProgressPercentage { get; set; }
        public int TodayDoneMinutes { get; set; }
        public int? DailyTargetMinutesForToday { get; set; }
        public double TodayProgressPercentage { get; set; }
        public bool IsTodayAchieved { get; set; }
        public int StreakDays { get; set; }
    }
}
