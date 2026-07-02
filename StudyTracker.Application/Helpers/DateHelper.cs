namespace StudyTracker.Helpers
{
    public static class DateHelper
    {
        public static DateOnly GetWeekStartDate(DateOnly date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            // Saturday = 6, so we adjust to make Saturday the start of the week
            // If today is Saturday, return today. Otherwise, go back to the previous Saturday
            // Sunday = 0 -> go back 1 day, Monday = 1 -> go back 2 days, etc.
            var daysToSubtract = dayOfWeek == 6 ? 0 : (dayOfWeek + 1);
            return date.AddDays(-daysToSubtract);
        }

        public static DateOnly GetMonthStartDate(DateOnly date)
        {
            return new DateOnly(date.Year, date.Month, 1);
        }

        public static List<DateOnly> GetWeekDays(DateOnly weekStartDate)
        {
            var days = new List<DateOnly>();
            for (int i = 0; i < 7; i++)
            {
                days.Add(weekStartDate.AddDays(i));
            }
            return days;
        }
    }
}
