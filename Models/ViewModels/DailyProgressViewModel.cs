namespace StudyTracker.Models.ViewModels
{
    public class DailyProgressViewModel
    {
        public DateOnly SelectedDate { get; set; }
        public DateOnly WeekStartDate { get; set; }
        public DateOnly WeekEndDate { get; set; }

        public int DailyTargetMinutes { get; set; }
        public int StudiedMinutes { get; set; }
        public int SessionsCount { get; set; }
        public int CurrentRank { get; set; }

        public int StreakDays { get; set; }

        public int WeeklyTotalMinutes { get; set; }
        public double WeeklyAverageMinutesPerDay { get; set; }
        public int PreviousWeekTotalMinutes { get; set; }

        public List<DailyProgressDaySummaryViewModel> WeekDays { get; set; } = new();
        public List<DailyProgressSessionViewModel> Sessions { get; set; } = new();
        public List<DailyProgressTodoItemViewModel> Todos { get; set; } = new();

        public Dictionary<int, int> ProductiveMinutesByHour { get; set; } = new();

        public bool HasDailyTarget => DailyTargetMinutes > 0;
        public double ProgressPercentage => DailyTargetMinutes > 0 ? (StudiedMinutes * 100.0 / DailyTargetMinutes) : 0;
        public bool IsAchieved => DailyTargetMinutes > 0 && StudiedMinutes >= DailyTargetMinutes;
        /// <summary>Display label for daily target status: "No target set", "Achieved", or "In progress".</summary>
        public string DailyTargetStatusLabel => !HasDailyTarget ? "No target set" : (IsAchieved ? "Achieved" : "In progress");
        public double StudiedHours => StudiedMinutes / 60.0;
        public int AverageSessionMinutes => SessionsCount > 0 ? (int)Math.Round(StudiedMinutes / (double)SessionsCount) : 0;
    }

    public class DailyProgressDaySummaryViewModel
    {
        public DateOnly Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int StudiedMinutes { get; set; }
        public int TargetMinutes { get; set; }
        public bool IsAchieved => TargetMinutes > 0 && StudiedMinutes >= TargetMinutes;
    }

    public class DailyProgressSessionViewModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int DurationMinutes { get; set; }
        public string? Notes { get; set; }
    }

    public class DailyProgressTodoItemViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}

