namespace StudyTracker.Models.ViewModels
{
    public class LeaderboardEntryViewModel
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public double TotalHours { get; set; }
        public int SessionsCount { get; set; }
        public bool IsCurrentUser { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
