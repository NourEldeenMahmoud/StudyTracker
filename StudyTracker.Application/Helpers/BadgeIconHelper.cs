using System.Text.RegularExpressions;

namespace StudyTracker.Helpers
{
    /// <summary>Detects Material Symbols ligature names (allows digits, e.g. <c>filter_3</c>).</summary>
    public static class BadgeIconHelper
    {
        private static readonly Regex MaterialSymbolName = new(@"^[a-z0-9_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsMaterialSymbolName(string? icon) =>
            !string.IsNullOrEmpty(icon) && MaterialSymbolName.IsMatch(icon);
    }
}
