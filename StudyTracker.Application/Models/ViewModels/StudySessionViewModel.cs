namespace StudyTracker.Models.ViewModels
{
    public class StudySessionViewModel
    {
        public Guid Id { get; set; }
        public DateOnly Date { get; set; }
        public int DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public double Hours => DurationMinutes / 60.0;
        public string FormattedDuration => $"{DurationMinutes / 60}h {DurationMinutes % 60}m";
    }
}
