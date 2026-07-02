using Microsoft.EntityFrameworkCore;
using StudyTracker.Models;

namespace StudyTracker.Data
{
    public interface IApplicationDbContext
    {
        DbSet<StudySession> StudySessions { get; }
        DbSet<UserWeeklyTarget> UserWeeklyTargets { get; }
        DbSet<TodoItem> TodoItems { get; }
        DbSet<UserBadge> UserBadges { get; }
        DbSet<CustomBadgeDefinition> CustomBadgeDefinitions { get; }
        DbSet<BadgeDefinitionOverride> BadgeDefinitionOverrides { get; }
        DbSet<SecurityLog> SecurityLogs { get; }
        DbSet<WeeklyLeaderboardArchive> WeeklyLeaderboardArchives { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<ApplicationUser> Users { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
    }
}
