namespace StudyTracker.Models.ViewModels
{
    public class AdminSessionViewModel
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public int DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public double Hours => DurationMinutes / 60.0;
        public string FormattedDuration => $"{DurationMinutes / 60}h {DurationMinutes % 60}m";
    }
}
