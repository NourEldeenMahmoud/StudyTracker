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
        public List<LeaderboardBadgeInfo> Badges { get; set; } = new();
    }

    public class LeaderboardBadgeInfo
    {
        public string Icon        { get; set; } = string.Empty;
        public string Color       { get; set; } = string.Empty;
        public string Name        { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>System tier (Roman / ring); null for custom badges.</summary>
        public int? TierLevel     { get; set; }
        /// <summary>e.g. hours, streak — drives tier color ramp.</summary>
        public string? CategoryId { get; set; }
    }
}
