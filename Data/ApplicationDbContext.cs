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
        }
    }
}
