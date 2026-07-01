using StudyTracker.Services;

namespace StudyTracker.Models.ViewModels
{
    public class BadgeSubgroupViewModel
    {
        public string SubgroupTitle { get; set; } = "";
        public BadgeDefinition? CurrentTier { get; set; }
        public UserBadge? CurrentBadge { get; set; }
        public BadgeDefinition? NextTier { get; set; }
        /// <summary>0–100 when numeric progress applies (hours / streak).</summary>
        public double? ProgressPercent { get; set; }
        public string? ProgressLabel { get; set; }
    }

    public class BadgeGroupViewModel
    {
        public string Title { get; set; } = "";
        public List<BadgeSubgroupViewModel> Subgroups { get; set; } = new();
    }
}
