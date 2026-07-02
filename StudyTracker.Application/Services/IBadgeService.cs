using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    /// <param name="CategoryId">Tier ladder id; empty for non-tiered (unused today).</param>
    /// <param name="TierLevel">1 = lowest within category; higher = better.</param>
    /// <param name="DisplayGroupKey">Profile/admin section (e.g. Hours, Streak, Ranking).</param>
    /// <param name="DisplaySubgroup">Optional second heading under the group (e.g. two ranking tracks).</param>
    public record BadgeDefinition(
        string Key,
        string Name,
        string Description,
        string Icon,
        string Color,
        string CategoryId = "",
        int TierLevel = 0,
        string DisplayGroupKey = "",
        string DisplaySubgroup = ""
    );

    public interface IBadgeService
    {
        /// <summary>Check all badge conditions and award any newly earned badges. Returns newly earned badges.</summary>
        Task<List<UserBadge>> CheckAndAwardAsync(string userId);

        /// <summary>Get all badges a user has earned.</summary>
        Task<List<UserBadge>> GetUserBadgesAsync(string userId);

        /// <summary>Get all badge definitions (for display including unearned ones).</summary>
        IReadOnlyList<BadgeDefinition> GetAllDefinitions();

        /// <summary>Get all badge definitions merged with admin overrides (for display).</summary>
        Task<IReadOnlyList<BadgeDefinition>> GetAllDefinitionsAsync();

        /// <summary>Admin: save override for a system badge's name, description, icon, color.</summary>
        Task SaveSystemBadgeOverrideAsync(string badgeKey, string name, string description, string icon, string color);

        /// <summary>Admin: manually assign a badge to a user. Returns false if already earned.</summary>
        Task<bool> AssignBadgeAsync(string userId, string badgeKey);

        /// <summary>Admin: revoke a badge from a user. Returns false if not found.</summary>
        Task<bool> RevokeBadgeAsync(string userId, string badgeKey);

        /// <summary>Admin: get all users with their earned badges.</summary>
        Task<List<UserBadgeSummary>> GetAllUserBadgeSummariesAsync();

        /// <summary>Mark specific badges as notified (so they don't show again on next page load).</summary>
        Task MarkAsNotifiedAsync(IEnumerable<Guid> badgeIds);

        // ── Custom badge definitions (admin-only, manual assignment) ──────

        Task<List<StudyTracker.Models.CustomBadgeDefinition>> GetCustomDefinitionsAsync();
        Task<StudyTracker.Models.CustomBadgeDefinition> CreateCustomBadgeAsync(string name, string description, string icon, string color, string? adminNote, string adminUserId);
        Task<StudyTracker.Models.CustomBadgeDefinition?> UpdateCustomBadgeAsync(Guid id, string name, string description, string icon, string color, string? adminNote);
        Task<bool> DeleteCustomBadgeAsync(Guid id);

        /// <summary>Assign a custom badge (by its GUID key) or a predefined badge (by its string key).</summary>
        Task<bool> AssignCustomBadgeAsync(string userId, Guid customBadgeId);

        /// <summary>Idempotent: per user and tier category, keep only the highest-tier UserBadge row.</summary>
        Task<int> NormalizeTieredUserBadgesAsync();

        /// <summary>Grouped badges for profile/public: current tier, next tier preview, optional progress.</summary>
        Task<IReadOnlyList<BadgeGroupViewModel>> GetProfileBadgeGroupsAsync(string userId);
    }

    public record UserBadgeSummary(
        string UserId,
        string FullName,
        string? Email,
        List<UserBadge> Badges
    );
}
