namespace StudyTracker.Models.ViewModels
{
    public class AdminDashboardStatsViewModel
    {
        public int TotalStudents { get; set; }
        public int ActiveSessionsToday { get; set; }
        public double TotalHoursLogged { get; set; }
        public string? TopUserWeeklyName { get; set; }
        public double TopUserWeeklyHours { get; set; }
        public List<LeaderboardEntryViewModel> WeeklyTop5 { get; set; } = new List<LeaderboardEntryViewModel>();
    }
}
