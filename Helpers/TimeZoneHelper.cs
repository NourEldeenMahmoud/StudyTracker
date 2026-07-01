namespace StudyTracker.Helpers
{
    /// <summary>
    /// Provides "today" and "now" in Africa/Cairo timezone for consistent date boundaries across the app.
    /// </summary>
    public static class TimeZoneHelper
    {
        private static readonly TimeZoneInfo CairoTimeZone = GetCairoTimeZone();

        private static TimeZoneInfo GetCairoTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                }
                catch
                {
                    return TimeZoneInfo.Utc;
                }
            }
        }

        /// <summary>
        /// Current date in Cairo (for "today" in business logic: streak, sessions, leaderboard).
        /// </summary>
        public static DateOnly GetTodayInCairo()
        {
            var nowInCairo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CairoTimeZone);
            return DateOnly.FromDateTime(nowInCairo);
        }

        /// <summary>
        /// Current date/time in Cairo (for display or local-time logic).
        /// </summary>
        public static DateTime GetNowInCairo()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CairoTimeZone);
        }

        /// <summary>
        /// Convert a UTC DateTime to Cairo local time.
        /// </summary>
        public static DateTime ToCairo(DateTime utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utc, CairoTimeZone);
        }
    }
}
