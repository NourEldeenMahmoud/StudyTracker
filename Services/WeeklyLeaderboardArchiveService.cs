using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public class WeeklyLeaderboardArchiveService : IWeeklyLeaderboardArchiveService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILeaderboardService _leaderboardService;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public WeeklyLeaderboardArchiveService(ApplicationDbContext context, ILeaderboardService leaderboardService)
        {
            _context = context;
            _leaderboardService = leaderboardService;
        }

        public async Task ArchiveWeekIfMissingAsync(DateOnly weekStart)
        {
            var existing = await _context.WeeklyLeaderboardArchives
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.WeekStartDate == weekStart);
            if (existing != null) return;

            var entries = await _leaderboardService.GetWeeklyLeaderboardAsync(weekStart);
            var snapshot = entries
                .Select(e => new
                {
                    e.Rank,
                    e.UserId,
                    e.FullName,
                    e.TotalHours,
                    e.SessionsCount
                })
                .ToList();

            var archive = new WeeklyLeaderboardArchive
            {
                WeekStartDate = weekStart,
                WeekEndDate = weekStart.AddDays(6),
                SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions),
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.WeeklyLeaderboardArchives.Add(archive);
            await _context.SaveChangesAsync();
        }

        public async Task<List<WeeklyLeaderboardArchive>> GetArchivesAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 20;

            return await _context.WeeklyLeaderboardArchives
                .AsNoTracking()
                .OrderByDescending(a => a.WeekStartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<WeeklyLeaderboardArchive?> GetByWeekAsync(DateOnly weekStart)
        {
            return await _context.WeeklyLeaderboardArchives
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.WeekStartDate == weekStart);
        }

        public async Task<List<LeaderboardEntryViewModel>> GetEntriesForArchiveAsync(WeeklyLeaderboardArchive archive)
        {
            if (archive == null || string.IsNullOrWhiteSpace(archive.SnapshotJson))
                return new List<LeaderboardEntryViewModel>();

            var snapshot = JsonSerializer.Deserialize<List<SnapshotRow>>(archive.SnapshotJson, JsonOptions) ?? new();

            // Optionally hydrate profile picture from current Users table
            var userIds = snapshot.Select(s => s.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.ProfilePictureUrl })
                .ToListAsync();

            return snapshot
                .OrderBy(s => s.Rank)
                .Select(s =>
                {
                    var user = users.FirstOrDefault(u => u.Id == s.UserId);
                    return new LeaderboardEntryViewModel
                    {
                        Rank = s.Rank,
                        UserId = s.UserId,
                        FullName = s.FullName,
                        TotalHours = s.TotalHours,
                        SessionsCount = s.SessionsCount,
                        ProfilePictureUrl = user?.ProfilePictureUrl
                    };
                })
                .ToList();
        }

        public async Task<bool> WasUserEverRankOneAsync(string userId)
        {
            // Only consider completed (archived) weeks so the badge is awarded
            // to official weekly winners, not to users who are temporarily #1
            // during the current week.
            var archives = await _context.WeeklyLeaderboardArchives
                .AsNoTracking()
                .OrderBy(a => a.WeekStartDate)
                .ToListAsync();

            foreach (var archive in archives)
            {
                if (string.IsNullOrWhiteSpace(archive.SnapshotJson))
                    continue;

                var snapshot = JsonSerializer.Deserialize<List<SnapshotRow>>(archive.SnapshotJson, JsonOptions);
                var first = snapshot?.FirstOrDefault(s => s.Rank == 1);
                if (first?.UserId == userId)
                    return true;
            }

            return false;
        }

        public async Task<bool> WasUserEverRankExactlyAsync(string userId, int rank)
        {
            if (rank < 1)
                return false;

            var archives = await _context.WeeklyLeaderboardArchives
                .AsNoTracking()
                .OrderBy(a => a.WeekStartDate)
                .ToListAsync();

            foreach (var archive in archives)
            {
                if (string.IsNullOrWhiteSpace(archive.SnapshotJson))
                    continue;

                var snapshot = JsonSerializer.Deserialize<List<SnapshotRow>>(archive.SnapshotJson, JsonOptions);
                var row = snapshot?.FirstOrDefault(s => s.UserId == userId && s.Rank == rank);
                if (row != null)
                    return true;
            }

            return false;
        }

        public async Task<int> GetMaxConsecutiveWeeksInTopAsync(string userId, int topN)
        {
            var today = TimeZoneHelper.GetTodayInCairo();
            var currentWeekStart = DateHelper.GetWeekStartDate(today);

            // Build list of (WeekStartDate, Rank) for user; rank 0 = not in top N
            var weekRanks = new List<(DateOnly WeekStart, int Rank)>();

            // Current week
            var currentLeaderboard = await _leaderboardService.GetWeeklyLeaderboardAsync(currentWeekStart);
            var currentEntry = currentLeaderboard.FirstOrDefault(e => e.UserId == userId);
            weekRanks.Add((currentWeekStart, currentEntry?.Rank ?? 0));

            var archives = await _context.WeeklyLeaderboardArchives
                .AsNoTracking()
                .OrderByDescending(a => a.WeekStartDate)
                .ToListAsync();

            foreach (var archive in archives)
            {
                if (archive.WeekStartDate == currentWeekStart) continue; // already added
                if (string.IsNullOrWhiteSpace(archive.SnapshotJson)) continue;
                var snapshot = JsonSerializer.Deserialize<List<SnapshotRow>>(archive.SnapshotJson, JsonOptions);
                var entry = snapshot?.FirstOrDefault(s => s.UserId == userId);
                var rank = entry?.Rank ?? 0;
                weekRanks.Add((archive.WeekStartDate, rank));
            }

            // Sort by week ascending (oldest first) to compute consecutive
            weekRanks = weekRanks.OrderBy(w => w.WeekStart).ToList();

            int maxStreak = 0;
            int currentStreak = 0;
            foreach (var (_, rank) in weekRanks)
            {
                if (rank >= 1 && rank <= topN)
                {
                    currentStreak++;
                    if (currentStreak > maxStreak) maxStreak = currentStreak;
                }
                else
                    currentStreak = 0;
            }

            return maxStreak;
        }

        private class SnapshotRow
        {
            public int Rank { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public double TotalHours { get; set; }
            public int SessionsCount { get; set; }
        }
    }
}

