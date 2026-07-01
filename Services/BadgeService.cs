using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public class BadgeService : IBadgeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWeeklyLeaderboardArchiveService _archiveService;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>All system tier definitions (thresholds and copy live in code; deploy to change).</summary>
        private static readonly List<BadgeDefinition> Definitions = new()
        {
            new("streak_7",                "Week Warrior",        "7-day study streak",                                  "local_fire_department",   "text-orange-500 bg-orange-100 dark:bg-orange-900/30",     "streak",            1, "Streak",  ""),
            new("streak_14",               "Fortnight Focus",     "14-day study streak",                                 "whatshot",                "text-rose-500 bg-rose-100 dark:bg-rose-900/30",           "streak",            2, "Streak",  ""),
            new("streak_30",               "Monthly Master",      "30-day study streak",                                 "military_tech",           "text-purple-500 bg-purple-100 dark:bg-purple-900/30",     "streak",            3, "Streak",  ""),
            new("streak_60",               "Two-Month Flame",     "60-day study streak",                                 "calendar_month",          "text-pink-500 bg-pink-100 dark:bg-pink-900/30",           "streak",            4, "Streak",  ""),
            new("streak_120",              "Four-Month Forge",    "120-day study streak",                                "volcano",                 "text-fuchsia-500 bg-fuchsia-100 dark:bg-fuchsia-900/30",  "streak",            5, "Streak",  ""),
            new("leaderboard_third_once",  "Bronze Podium",       "Finished 3rd on a completed weekly leaderboard",      "filter_3",                "text-amber-700 bg-amber-100 dark:bg-amber-900/30",        "ranking_rank_one", 1, "Ranking", ""),
            new("leaderboard_second_once", "Silver Podium",       "Finished 2nd on a completed weekly leaderboard",      "filter_2",                "text-amber-600 bg-amber-50 dark:bg-amber-900/25",         "ranking_rank_one", 2, "Ranking", ""),
            new("leaderboard_first_once",  "Champion",            "Won the weekly leaderboard in a completed week",    "emoji_events",            "text-yellow-600 bg-yellow-100 dark:bg-yellow-900/30",     "ranking_rank_one",  3, "Ranking", ""),
            new("hours_100",               "100 Hours",           "Reached 100 total study hours",                       "timer",                   "text-cyan-500 bg-cyan-100 dark:bg-cyan-900/30",             "hours",             1, "Hours",   ""),
            new("hours_200",               "200 Hours",           "Reached 200 total study hours",                       "workspace_premium",       "text-emerald-500 bg-emerald-100 dark:bg-emerald-900/30",  "hours",             2, "Hours",   ""),
            new("hours_500",               "500 Hours",           "Reached 500 total study hours",                       "trophy",                  "text-yellow-500 bg-yellow-100 dark:bg-yellow-900/30",     "hours",             3, "Hours",   ""),
            new("hours_1000",              "1000 Hours",          "Reached 1000 total study hours",                      "diamond",                 "text-indigo-500 bg-indigo-100 dark:bg-indigo-900/30",     "hours",             4, "Hours",   "")
        };

        private static readonly Dictionary<string, HashSet<string>> KeysByCategory = Definitions
            .GroupBy(d => d.CategoryId)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase));


        public BadgeService(ApplicationDbContext context, IWeeklyLeaderboardArchiveService archiveService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _archiveService = archiveService;
            _userManager = userManager;
        }

        public IReadOnlyList<BadgeDefinition> GetAllDefinitions() => Definitions;

        public async Task<IReadOnlyList<BadgeDefinition>> GetAllDefinitionsAsync()
        {
            var overrides = await _context.BadgeDefinitionOverrides
                .AsNoTracking()
                .ToDictionaryAsync(o => o.BadgeKey, StringComparer.OrdinalIgnoreCase);

            return Definitions
                .Select(d =>
                {
                    if (overrides.TryGetValue(d.Key, out var ov))
                        return new BadgeDefinition(d.Key, ov.Name, ov.Description, ov.Icon, ov.Color, d.CategoryId, d.TierLevel, d.DisplayGroupKey, d.DisplaySubgroup);
                    return d;
                })
                .ToList();
        }

        public async Task SaveSystemBadgeOverrideAsync(string badgeKey, string name, string description, string icon, string color)
        {
            var keyExists = Definitions.Any(d => string.Equals(d.Key, badgeKey, StringComparison.OrdinalIgnoreCase));
            if (!keyExists)
                return;

            var ov = await _context.BadgeDefinitionOverrides.FirstOrDefaultAsync(o => o.BadgeKey == badgeKey);
            if (ov == null)
            {
                ov = new BadgeDefinitionOverride
                {
                    BadgeKey = badgeKey,
                    Name = name,
                    Description = description ?? "",
                    Icon = icon,
                    Color = color,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _context.BadgeDefinitionOverrides.Add(ov);
            }
            else
            {
                ov.Name = name;
                ov.Description = description ?? "";
                ov.Icon = icon;
                ov.Color = color;
                ov.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<UserBadge>> GetUserBadgesAsync(string userId)
        {
            return await _context.UserBadges
                .Where(b => b.UserId == userId)
                .OrderBy(b => b.EarnedAt)
                .ToListAsync();
        }

        public async Task<List<UserBadge>> CheckAndAwardAsync(string userId)
        {
            var totalMinutes = await GetTotalMinutesAsync(userId);
            var streakDays = await GetCurrentStreakAsync(userId);
            var everRankOne = await _archiveService.WasUserEverRankOneAsync(userId);
            var everRankTwo = await _archiveService.WasUserEverRankExactlyAsync(userId, 2);
            var everRankThree = await _archiveService.WasUserEverRankExactlyAsync(userId, 3);

            var newBadges = new List<UserBadge>();

            foreach (var cat in KeysByCategory.Keys)
            {
                var eligibleKey = GetHighestEligibleKeyInCategory(cat, totalMinutes, streakDays, everRankOne, everRankTwo, everRankThree);
                var storedMaxTier = await GetStoredMaxTierAsync(userId, cat);

                var eligibleTier = eligibleKey == null ? 0 : Definitions.First(d => d.Key == eligibleKey).TierLevel;

                if (eligibleTier <= storedMaxTier)
                    continue;

                var keysInCat = KeysByCategory[cat];
                var toRemove = await _context.UserBadges
                    .Where(b => b.UserId == userId && keysInCat.Contains(b.BadgeKey))
                    .ToListAsync();
                foreach (var r in toRemove)
                    _context.UserBadges.Remove(r);

                if (eligibleKey == null)
                    continue;

                var badge = new UserBadge
                {
                    UserId = userId,
                    BadgeKey = eligibleKey,
                    EarnedAt = DateTime.UtcNow,
                    IsNotified = false
                };
                _context.UserBadges.Add(badge);
                newBadges.Add(badge);
            }

            if (newBadges.Any())
                await _context.SaveChangesAsync();

            return newBadges;
        }

        private static string? GetHighestEligibleKeyInCategory(
            string categoryId,
            int totalMinutes,
            int streakDays,
            bool everRankOne,
            bool everRankTwo,
            bool everRankThree)
        {
            var tiers = Definitions
                .Where(d => d.CategoryId == categoryId)
                .OrderBy(d => d.TierLevel)
                .ToList();

            BadgeDefinition? best = null;
            foreach (var def in tiers)
            {
                if (ConditionMet(def.Key, totalMinutes, streakDays, everRankOne, everRankTwo, everRankThree))
                    best = def;
            }

            return best?.Key;
        }

        private static bool ConditionMet(string key, int totalMinutes, int streakDays, bool everRankOne, bool everRankTwo, bool everRankThree)
        {
            return key switch
            {
                "streak_7" => streakDays >= 7,
                "streak_14" => streakDays >= 14,
                "streak_30" => streakDays >= 30,
                "streak_60" => streakDays >= 60,
                "streak_120" => streakDays >= 120,
                "leaderboard_third_once" => everRankThree,
                "leaderboard_second_once" => everRankTwo,
                "leaderboard_first_once" => everRankOne,
                "hours_100" => totalMinutes >= 6000,
                "hours_200" => totalMinutes >= 12000,
                "hours_500" => totalMinutes >= 30000,
                "hours_1000" => totalMinutes >= 60000,
                _ => false
            };
        }

        private async Task<int> GetStoredMaxTierAsync(string userId, string categoryId)
        {
            var keys = KeysByCategory[categoryId];
            var rows = await _context.UserBadges
                .Where(b => b.UserId == userId && keys.Contains(b.BadgeKey))
                .Select(b => b.BadgeKey)
                .ToListAsync();

            var max = 0;
            foreach (var k in rows)
            {
                var def = Definitions.FirstOrDefault(d => string.Equals(d.Key, k, StringComparison.OrdinalIgnoreCase));
                if (def != null && def.TierLevel > max)
                    max = def.TierLevel;
            }

            return max;
        }

        private async Task<int> GetCurrentStreakAsync(string userId)
        {
            var today = TimeZoneHelper.GetTodayInCairo();

            var sessionDates = await _context.StudySessions
                .Where(s => s.UserId == userId)
                .Select(s => s.Date)
                .Distinct()
                .ToListAsync();

            var dateSet = new HashSet<DateOnly>(sessionDates);
            var startDate = dateSet.Contains(today) ? today : today.AddDays(-1);
            var streak = 0;
            var checkDate = startDate;

            while (dateSet.Contains(checkDate))
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }

            return streak;
        }

        private async Task<int> GetTotalMinutesAsync(string userId)
        {
            return await _context.StudySessions
                .Where(s => s.UserId == userId)
                .SumAsync(s => (int?)s.DurationMinutes) ?? 0;
        }

        public async Task<bool> AssignBadgeAsync(string userId, string badgeKey)
        {
            var def = Definitions.FirstOrDefault(d => string.Equals(d.Key, badgeKey, StringComparison.OrdinalIgnoreCase));
            if (def == null || string.IsNullOrEmpty(def.CategoryId))
                return false;

            var keysInCat = KeysByCategory[def.CategoryId];
            var existing = await _context.UserBadges
                .Where(b => b.UserId == userId && keysInCat.Contains(b.BadgeKey))
                .ToListAsync();

            if (existing.Count == 1 && string.Equals(existing[0].BadgeKey, badgeKey, StringComparison.OrdinalIgnoreCase))
                return false;

            foreach (var r in existing)
                _context.UserBadges.Remove(r);

            _context.UserBadges.Add(new UserBadge
            {
                UserId = userId,
                BadgeKey = def.Key,
                EarnedAt = DateTime.UtcNow,
                IsNotified = false
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokeBadgeAsync(string userId, string badgeKey)
        {
            var badge = await _context.UserBadges
                .FirstOrDefaultAsync(b => b.UserId == userId && b.BadgeKey == badgeKey);
            if (badge == null) return false;

            _context.UserBadges.Remove(badge);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task MarkAsNotifiedAsync(IEnumerable<Guid> badgeIds)
        {
            var ids = badgeIds.ToHashSet();
            var badges = await _context.UserBadges
                .Where(b => ids.Contains(b.Id))
                .ToListAsync();
            foreach (var b in badges)
                b.IsNotified = true;
            if (badges.Any())
                await _context.SaveChangesAsync();
        }

        public async Task<List<UserBadgeSummary>> GetAllUserBadgeSummariesAsync()
        {
            var allBadges = await _context.UserBadges
                .Include(b => b.User)
                .OrderBy(b => b.EarnedAt)
                .ToListAsync();

            var users = await _userManager.Users.ToListAsync();

            return users
                .OrderBy(u => u.FullName)
                .Select(u => new UserBadgeSummary(
                    u.Id,
                    u.FullName ?? u.Email ?? "Unknown",
                    u.Email,
                    allBadges.Where(b => b.UserId == u.Id).ToList()
                ))
                .ToList();
        }

        public async Task<int> NormalizeTieredUserBadgesAsync()
        {
            var systemKeys = Definitions.Select(d => d.Key).ToList();
            var all = await _context.UserBadges
                .Where(b => systemKeys.Contains(b.BadgeKey))
                .ToListAsync();

            var byUser = all.GroupBy(b => b.UserId);
            var toRemove = new List<UserBadge>();

            foreach (var ug in byUser)
            {
                foreach (var cat in KeysByCategory.Keys)
                {
                    var keys = KeysByCategory[cat];
                    var inCat = ug.Where(b => keys.Contains(b.BadgeKey)).ToList();
                    if (inCat.Count <= 1)
                        continue;

                    var best = inCat
                        .Select(b => (b, Definitions.First(d => string.Equals(d.Key, b.BadgeKey, StringComparison.OrdinalIgnoreCase)).TierLevel))
                        .OrderByDescending(x => x.Item2)
                        .First().b;

                    foreach (var row in inCat)
                    {
                        if (row.Id != best.Id)
                            toRemove.Add(row);
                    }
                }
            }

            if (!toRemove.Any())
                return 0;

            _context.UserBadges.RemoveRange(toRemove);
            await _context.SaveChangesAsync();
            return toRemove.Count;
        }

        public async Task<IReadOnlyList<BadgeGroupViewModel>> GetProfileBadgeGroupsAsync(string userId)
        {
            // Tier badges are normally awarded on session add; sync here so profile/public views stay
            // consistent if sessions existed before badges, admin edits, or a prior CheckAndAward failed.
            try
            {
                await CheckAndAwardAsync(userId);
            }
            catch
            {
                /* profile should still render */
            }

            var defs = await GetAllDefinitionsAsync();
            var earned = await GetUserBadgesAsync(userId);
            var totalMinutes = await GetTotalMinutesAsync(userId);
            var streakDays = await GetCurrentStreakAsync(userId);
            var totalHours = totalMinutes / 60.0;

            var earnedByKey = earned.ToDictionary(b => b.BadgeKey, StringComparer.OrdinalIgnoreCase);

            var tiered = defs
                .Where(d => !string.IsNullOrEmpty(d.CategoryId))
                .GroupBy(d => (d.DisplayGroupKey, d.DisplaySubgroup))
                .OrderBy(g => GroupOrder(g.Key.DisplayGroupKey))
                .ThenBy(g => g.Key.DisplaySubgroup)
                .ToList();

            var result = new List<BadgeGroupViewModel>();
            BadgeGroupViewModel? currentGroup = null;

            foreach (var g in tiered)
            {
                var title = g.Key.DisplayGroupKey;
                if (currentGroup == null || currentGroup.Title != title)
                {
                    currentGroup = new BadgeGroupViewModel { Title = title };
                    result.Add(currentGroup);
                }

                var ordered = g.OrderBy(d => d.TierLevel).ToList();
                var categoryId = ordered[0].CategoryId;
                var keys = KeysByCategory[categoryId];

                BadgeDefinition? currentDef = null;
                UserBadge? currentBadge = null;
                var bestTier = 0;
                foreach (var d in ordered)
                {
                    if (earnedByKey.TryGetValue(d.Key, out var ub))
                    {
                        if (d.TierLevel >= bestTier)
                        {
                            bestTier = d.TierLevel;
                            currentDef = d;
                            currentBadge = ub;
                        }
                    }
                }

                BadgeDefinition? nextDef = ordered.FirstOrDefault(d => d.TierLevel > bestTier);

                double? progress = null;
                string? progressLabel = null;
                if (nextDef != null)
                {
                    if (categoryId == "hours")
                    {
                        var need = ThresholdMinutes(nextDef.Key);
                        if (need > 0)
                        {
                            progress = Math.Clamp(100.0 * totalMinutes / need, 0, 100);
                            progressLabel = $"{totalHours:F1} / {need / 60.0:F0} h";
                        }
                    }
                    else if (categoryId == "streak")
                    {
                        var need = ThresholdStreakDays(nextDef.Key);
                        if (need > 0)
                        {
                            progress = Math.Clamp(100.0 * streakDays / need, 0, 100);
                            progressLabel = $"{streakDays} / {need} days";
                        }
                    }
                }

                currentGroup.Subgroups.Add(new BadgeSubgroupViewModel
                {
                    SubgroupTitle = g.Key.DisplaySubgroup ?? "",
                    CurrentTier = currentDef,
                    CurrentBadge = currentBadge,
                    NextTier = nextDef,
                    ProgressPercent = progress,
                    ProgressLabel = progressLabel
                });
            }

            return result;
        }

        private static int ThresholdMinutes(string key) => key switch
        {
            "hours_100" => 6000,
            "hours_200" => 12000,
            "hours_500" => 30000,
            "hours_1000" => 60000,
            _ => 0
        };

        private static int ThresholdStreakDays(string key) => key switch
        {
            "streak_7" => 7,
            "streak_14" => 14,
            "streak_30" => 30,
            "streak_60" => 60,
            "streak_120" => 120,
            _ => 0
        };

        private static int GroupOrder(string key) => key switch
        {
            "Hours" => 0,
            "Streak" => 1,
            "Ranking" => 2,
            _ => 99
        };

        public async Task<List<StudyTracker.Models.CustomBadgeDefinition>> GetCustomDefinitionsAsync()
        {
            return await _context.CustomBadgeDefinitions
                .OrderBy(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<StudyTracker.Models.CustomBadgeDefinition> CreateCustomBadgeAsync(
            string name, string description, string icon, string color, string? adminNote, string adminUserId)
        {
            var def = new StudyTracker.Models.CustomBadgeDefinition
            {
                Name = name,
                Description = description,
                Icon = icon,
                Color = color,
                AdminNote = adminNote,
                CreatedByAdminId = adminUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.CustomBadgeDefinitions.Add(def);
            await _context.SaveChangesAsync();
            return def;
        }

        public async Task<StudyTracker.Models.CustomBadgeDefinition?> UpdateCustomBadgeAsync(
            Guid id, string name, string description, string icon, string color, string? adminNote)
        {
            var def = await _context.CustomBadgeDefinitions.FindAsync(id);
            if (def == null) return null;

            def.Name = name;
            def.Description = description;
            def.Icon = icon;
            def.Color = color;
            def.AdminNote = adminNote;

            await _context.SaveChangesAsync();
            return def;
        }

        public async Task<bool> DeleteCustomBadgeAsync(Guid id)
        {
            var def = await _context.CustomBadgeDefinitions.FindAsync(id);
            if (def == null) return false;

            var instances = await _context.UserBadges
                .Where(b => b.BadgeKey == id.ToString())
                .ToListAsync();
            _context.UserBadges.RemoveRange(instances);
            _context.CustomBadgeDefinitions.Remove(def);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignCustomBadgeAsync(string userId, Guid customBadgeId)
        {
            var key = customBadgeId.ToString();
            var alreadyEarned = await _context.UserBadges
                .AnyAsync(b => b.UserId == userId && b.BadgeKey == key);
            if (alreadyEarned) return false;

            _context.UserBadges.Add(new StudyTracker.Models.UserBadge
            {
                UserId = userId,
                BadgeKey = key,
                EarnedAt = DateTime.UtcNow,
                IsNotified = false
            });
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
