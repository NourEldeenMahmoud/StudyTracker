using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudyTracker.Models;

namespace StudyTracker.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<StudySession> StudySessions { get; set; }
        public DbSet<UserWeeklyTarget> UserWeeklyTargets { get; set; }
        public DbSet<TodoItem> TodoItems { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<CustomBadgeDefinition> CustomBadgeDefinitions { get; set; }
        public DbSet<BadgeDefinitionOverride> BadgeDefinitionOverrides { get; set; }
        public DbSet<SecurityLog> SecurityLogs { get; set; }
        public DbSet<WeeklyLeaderboardArchive> WeeklyLeaderboardArchives { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure StudySession
            builder.Entity<StudySession>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => new { e.UserId, e.Date });
            });

            // Configure UserWeeklyTarget
            builder.Entity<UserWeeklyTarget>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.WeekStartDate);
                entity.HasIndex(e => new { e.UserId, e.WeekStartDate });
            });

            // Configure TodoItem
            builder.Entity<TodoItem>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.Date });
                entity.HasIndex(e => new { e.UserId, e.Order });
            });

            // Configure UserBadge — one user can earn each badge only once
            builder.Entity<UserBadge>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.BadgeKey }).IsUnique();
            });

            // Configure SecurityLog
            builder.Entity<SecurityLog>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAtUtc);
                entity.Property(e => e.Action).IsRequired();
                entity.Property(e => e.TargetType).IsRequired();
            });

            // Configure WeeklyLeaderboardArchive
            builder.Entity<WeeklyLeaderboardArchive>(entity =>
            {
                entity.HasIndex(e => e.WeekStartDate).IsUnique();
            });

            // Configure Notification
            builder.Entity<Notification>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.IsRead });
                entity.HasIndex(e => e.CreatedAtUtc);
            });
        }
    }
}
