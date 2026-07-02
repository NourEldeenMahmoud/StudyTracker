using System;
using System.Collections.Generic;

namespace StudyTracker.Helpers
{
    /// <summary>Roman tier + per-category hue with lighter → darker tiers.</summary>
    public static class BadgeTierUi
    {
        private static readonly Lazy<string> SafelistLazy = new(BuildTailwindSafelistClassString);

        /// <summary>All utility tokens from tier UI (for Tailwind Play CDN — classes built only in C# must appear in HTML).</summary>
        public static string TailwindSafelistClassString => SafelistLazy.Value;

        public static string ToRoman(int tier)
        {
            if (tier <= 0) return "";
            if (tier >= 40) return tier.ToString();
            var n = tier;
            var s = "";
            while (n >= 10) { s += "X"; n -= 10; }
            if (n >= 9) { s += "IX"; n -= 9; }
            if (n >= 5) { s += "V"; n -= 5; }
            if (n >= 4) { s += "IV"; n -= 4; }
            while (n >= 1) { s += "I"; n--; }
            return s;
        }

        /// <summary>Light edge only — tier hue lives on <see cref="IconBadgeSurfaceClasses"/>.</summary>
        public static string IconRingClasses(int tier, string? categoryId = null) =>
            "ring-1 ring-slate-900/[0.08] dark:ring-white/15";

        /// <summary>Icon circle / chip fill: same hue per category, lighter → darker by tier.</summary>
        public static string IconBadgeSurfaceClasses(int tier, string? categoryId, bool lockedPreview = false)
        {
            if (lockedPreview)
            {
                return categoryId switch
                {
                    "hours" => HoursSurfacePreview(tier),
                    "streak" => StreakSurfacePreview(tier),
                    "ranking_rank_one" => RankPodiumSurfacePreview(tier),
                    "ranking_top3" => "text-orange-600/75 bg-orange-50/90 dark:bg-orange-400/15 dark:text-orange-300 dark:ring-1 dark:ring-orange-400/35",
                    _ => NeutralSurfacePreview(tier)
                };
            }

            return categoryId switch
            {
                "hours" => HoursSurface(tier),
                "streak" => StreakSurface(tier),
                "ranking_rank_one" => RankPodiumSurface(tier),
                "ranking_top3" => "text-orange-700 bg-orange-50 dark:bg-orange-400/15 dark:text-orange-300 dark:ring-1 dark:ring-orange-400/40",
                _ => NeutralSurface(tier)
            };
        }

        public static string TierPillClasses(int tier, string? categoryId = null) => categoryId switch
        {
            "hours" => HoursPill(tier),
            "streak" => StreakPill(tier),
            "ranking_rank_one" => RankPodiumPill(tier),
            "ranking_top3" => Top3Pill(tier),
            _ => NeutralPill(tier)
        };

        public static string LvBadgeClasses(int tier, string? categoryId = null) => categoryId switch
        {
            "hours" => HoursLv(tier),
            "streak" => StreakLv(tier),
            "ranking_rank_one" => RankPodiumLv(tier),
            "ranking_top3" => Top3Lv(tier),
            _ => "border border-slate-200 dark:border-slate-600 text-slate-500 dark:text-slate-300"
        };

        public static string TooltipTitle(string name, int? tierLevel)
        {
            if (tierLevel is int t && t > 0)
                return $"{name} — Tier {ToRoman(t)}";
            return name;
        }

        // Dark mode: solid bg (no /20 on 950) so chips stay readable on slate table rows; bright text for icons (currentColor).

        // ── Hours: cyan ───────────────────────────────────────────────
        private static string HoursSurface(int t) => t switch
        {
            1 => "text-cyan-700 bg-cyan-50 dark:bg-cyan-400/15 dark:text-cyan-300 dark:ring-1 dark:ring-cyan-500/40",
            2 => "text-cyan-800 bg-cyan-100 dark:bg-cyan-400/20 dark:text-cyan-200 dark:ring-1 dark:ring-cyan-400/45",
            3 => "text-cyan-900 bg-cyan-200/90 dark:bg-cyan-400/25 dark:text-cyan-100 dark:ring-1 dark:ring-cyan-300/40",
            4 => "text-cyan-950 bg-cyan-300/80 dark:bg-cyan-400/30 dark:text-white dark:ring-1 dark:ring-cyan-200/35",
            _ => NeutralSurface(t)
        };

        private static string HoursSurfacePreview(int t) => t switch
        {
            1 => "text-cyan-600 bg-cyan-50/90 dark:bg-slate-900 dark:text-cyan-200 dark:ring-1 dark:ring-cyan-600/40",
            2 => "text-cyan-700 bg-cyan-100/80 dark:bg-slate-900 dark:text-cyan-100 dark:ring-1 dark:ring-cyan-500/35",
            3 => "text-cyan-800 bg-cyan-200/70 dark:bg-slate-900 dark:text-cyan-50 dark:ring-1 dark:ring-cyan-400/35",
            4 => "text-cyan-900 bg-cyan-300/65 dark:bg-slate-900 dark:text-white dark:ring-1 dark:ring-cyan-300/30",
            _ => NeutralSurfacePreview(t)
        };

        private static string HoursPill(int t) => t switch
        {
            1 => "bg-cyan-100 text-cyan-900 dark:bg-cyan-400/20 dark:text-cyan-200 border border-cyan-200 dark:border-cyan-500/40",
            2 => "bg-cyan-200 text-cyan-950 dark:bg-cyan-400/25 dark:text-cyan-100 border border-cyan-300 dark:border-cyan-400/45",
            3 => "bg-cyan-400 text-cyan-950 dark:bg-cyan-400/30 dark:text-white border border-cyan-500 dark:border-cyan-400/50",
            4 => "bg-cyan-500 text-white dark:bg-cyan-400/35 dark:text-white border border-cyan-600 dark:border-cyan-300/50",
            _ => NeutralPill(t)
        };

        private static string HoursLv(int t) => t switch
        {
            1 => "border border-cyan-200 dark:border-cyan-600 text-cyan-800 dark:text-cyan-200",
            2 => "border border-cyan-300 dark:border-cyan-500 text-cyan-900 dark:text-cyan-100",
            3 => "border border-cyan-500 dark:border-cyan-400 text-cyan-950 dark:text-white",
            4 => "border border-cyan-600 dark:border-cyan-300 text-cyan-950 dark:text-cyan-50",
            _ => "border border-slate-200 dark:border-slate-600 text-slate-500 dark:text-slate-300"
        };

        // ── Streak: rose ───────────────────────────────────────────────
        private static string StreakSurface(int t) => t switch
        {
            1 => "text-rose-700 bg-rose-50 dark:bg-rose-400/15 dark:text-rose-300 dark:ring-1 dark:ring-rose-500/40",
            2 => "text-rose-800 bg-rose-100 dark:bg-rose-400/20 dark:text-rose-200 dark:ring-1 dark:ring-rose-400/45",
            3 => "text-rose-900 bg-rose-200/90 dark:bg-rose-400/25 dark:text-rose-100 dark:ring-1 dark:ring-rose-300/40",
            4 => "text-rose-950 bg-rose-300/80 dark:bg-rose-400/30 dark:text-rose-50 dark:ring-1 dark:ring-rose-200/35",
            5 => "text-rose-950 bg-rose-400/75 dark:bg-rose-400/35 dark:text-rose-50 dark:ring-1 dark:ring-rose-300/40",
            _ => NeutralSurface(t)
        };

        private static string StreakSurfacePreview(int t) => t switch
        {
            1 => "text-rose-600 bg-rose-50/90 dark:bg-slate-900 dark:text-rose-200 dark:ring-1 dark:ring-rose-600/40",
            2 => "text-rose-700 bg-rose-100/80 dark:bg-slate-900 dark:text-rose-100 dark:ring-1 dark:ring-rose-500/35",
            3 => "text-rose-800 bg-rose-200/70 dark:bg-slate-900 dark:text-rose-50 dark:ring-1 dark:ring-rose-400/35",
            4 => "text-rose-900 bg-rose-300/65 dark:bg-slate-900 dark:text-white dark:ring-1 dark:ring-rose-300/30",
            5 => "text-rose-950 bg-rose-400/60 dark:bg-slate-900 dark:text-rose-50 dark:ring-1 dark:ring-rose-300/30",
            _ => NeutralSurfacePreview(t)
        };

        private static string StreakPill(int t) => t switch
        {
            1 => "bg-rose-100 text-rose-900 dark:bg-rose-400/20 dark:text-rose-200 border border-rose-200 dark:border-rose-500/40",
            2 => "bg-rose-200 text-rose-950 dark:bg-rose-400/25 dark:text-rose-100 border border-rose-300 dark:border-rose-400/45",
            3 => "bg-rose-400 text-white dark:bg-rose-400/30 dark:text-white border border-rose-500 dark:border-rose-400/50",
            4 => "bg-rose-500 text-white dark:bg-rose-400/35 dark:text-rose-50 border border-rose-600 dark:border-rose-300/50",
            5 => "bg-rose-600 text-white dark:bg-rose-400/40 dark:text-rose-50 border border-rose-500 dark:border-rose-300/50",
            _ => NeutralPill(t)
        };

        private static string StreakLv(int t) => t switch
        {
            1 => "border border-rose-200 dark:border-rose-600 text-rose-800 dark:text-rose-200",
            2 => "border border-rose-300 dark:border-rose-500 text-rose-900 dark:text-rose-100",
            3 => "border border-rose-500 dark:border-rose-400 text-rose-950 dark:text-white",
            4 => "border border-rose-600 dark:border-rose-300 text-rose-950 dark:text-rose-50",
            5 => "border border-rose-600 dark:border-rose-300 text-rose-950 dark:text-rose-100",
            _ => "border border-slate-200 dark:border-slate-600 text-slate-500 dark:text-slate-300"
        };

        /// <summary>Weekly podium: tier 1 = #3, 2 = #2, 3 = #1.</summary>
        private static string RankPodiumSurface(int t) => t switch
        {
            1 => "text-stone-800 bg-stone-100 dark:bg-stone-400/15 dark:text-stone-300 dark:ring-1 dark:ring-stone-500/40",
            2 => "text-amber-900 bg-amber-100 dark:bg-amber-400/15 dark:text-amber-300 dark:ring-1 dark:ring-amber-400/45",
            3 => "text-amber-950 bg-amber-200/90 dark:bg-amber-400/20 dark:text-amber-200 dark:ring-1 dark:ring-amber-300/50",
            _ => NeutralSurface(t)
        };

        private static string RankPodiumSurfacePreview(int t) => t switch
        {
            1 => "text-stone-700 bg-stone-50 dark:bg-slate-900 dark:text-stone-200 dark:ring-1 dark:ring-stone-600/40",
            2 => "text-amber-800 bg-amber-50 dark:bg-slate-900 dark:text-amber-100 dark:ring-1 dark:ring-amber-600/40",
            3 => "text-amber-950 bg-amber-100/90 dark:bg-slate-900 dark:text-amber-50 dark:ring-1 dark:ring-amber-500/40",
            _ => NeutralSurfacePreview(t)
        };

        private static string RankPodiumPill(int t) => t switch
        {
            1 => "bg-stone-200 text-stone-900 dark:bg-stone-400/20 dark:text-stone-200 border border-stone-300 dark:border-stone-500/40",
            2 => "bg-amber-200 text-amber-950 dark:bg-amber-400/20 dark:text-amber-200 border border-amber-300 dark:border-amber-500/40",
            3 => "bg-amber-400 text-amber-950 dark:bg-amber-400/25 dark:text-amber-100 border border-amber-500 dark:border-amber-400/50",
            _ => NeutralPill(t)
        };

        private static string RankPodiumLv(int t) => t switch
        {
            1 => "border border-stone-300 dark:border-stone-500 text-stone-900 dark:text-stone-200",
            2 => "border border-amber-300 dark:border-amber-500 text-amber-950 dark:text-amber-100",
            3 => "border border-amber-500 dark:border-amber-400 text-amber-950 dark:text-amber-50",
            _ => "border border-slate-200 dark:border-slate-600 text-slate-500 dark:text-slate-300"
        };

        private static string Top3Pill(int _) =>
            "bg-orange-100 text-orange-900 dark:bg-orange-400/20 dark:text-orange-200 border border-orange-200 dark:border-orange-500/40";

        private static string Top3Lv(int _) =>
            "border border-orange-300 dark:border-orange-400/50 text-orange-900 dark:text-orange-300";

        private static string NeutralSurface(int t) => t switch
        {
            1 => "text-slate-700 bg-slate-50 dark:bg-slate-400/15 dark:text-slate-300 dark:ring-1 dark:ring-slate-600/50",
            2 => "text-slate-800 bg-slate-100 dark:bg-slate-400/15 dark:text-slate-200 dark:ring-1 dark:ring-slate-500/45",
            3 => "text-slate-900 bg-slate-200 dark:bg-slate-400/20 dark:text-white dark:ring-1 dark:ring-slate-400/35",
            4 => "text-slate-950 bg-slate-300 dark:bg-slate-400/25 dark:text-white dark:ring-1 dark:ring-slate-500/40",
            _ => "text-slate-700 bg-slate-50 dark:bg-slate-400/15 dark:text-slate-300 dark:ring-1 dark:ring-slate-600/50"
        };

        private static string NeutralSurfacePreview(int _) =>
            "text-slate-600 bg-slate-50 dark:bg-slate-900 dark:text-slate-200 dark:ring-1 dark:ring-slate-600/40";

        private static string NeutralPill(int t) => t switch
        {
            1 => "bg-slate-100 text-slate-800 dark:bg-slate-400/15 dark:text-slate-300 border border-slate-200 dark:border-slate-500/40",
            2 => "bg-slate-200 text-slate-900 dark:bg-slate-400/20 dark:text-slate-200 border border-slate-300 dark:border-slate-500/45",
            3 => "bg-slate-400 text-slate-950 dark:bg-slate-400/25 dark:text-white border border-slate-500 dark:border-slate-400/50",
            4 => "bg-slate-500 text-white dark:bg-slate-400/30 dark:text-white border border-slate-600 dark:border-slate-400/50",
            _ => "bg-slate-200 text-slate-900 dark:bg-slate-400/15 dark:text-slate-300"
        };

        private static string BuildTailwindSafelistClassString()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            void add(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return;
                foreach (var p in s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    set.Add(p);
            }

            add(IconRingClasses(1, null));
            var cats = new[] { "hours", "streak", "ranking_rank_one", "ranking_top3" };
            for (var t = 1; t <= 6; t++)
            {
                foreach (var c in cats)
                {
                    add(IconBadgeSurfaceClasses(t, c, false));
                    add(IconBadgeSurfaceClasses(t, c, true));
                    add(TierPillClasses(t, c));
                    add(LvBadgeClasses(t, c));
                    add(IconRingClasses(t, c));
                }

                add(IconBadgeSurfaceClasses(t, null, false));
                add(IconBadgeSurfaceClasses(t, null, true));
                add(TierPillClasses(t, null));
                add(LvBadgeClasses(t, null));
            }

            return string.Join(" ", set);
        }
    }
}
