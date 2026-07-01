namespace StudyTracker.Models.ViewModels
{
    /// <summary>Passed to _BadgeTierMark partial for Roman tier + category-based colors.</summary>
    public record BadgeTierMarkViewModel(int? TierLevel, string? CategoryId = null);
}
