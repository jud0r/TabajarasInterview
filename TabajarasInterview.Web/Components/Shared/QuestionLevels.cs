using MudBlazor;

namespace TabajarasInterview.Web.Components.Shared
{
    /// <summary>
    /// Central definition of the question difficulty levels supported by the UI,
    /// plus a defensive mapping to MudBlazor <see cref="Color"/> for badges/chips.
    /// Mirrors the pattern used by <see cref="StatusVisuals"/>.
    /// </summary>
    public static class QuestionLevels
    {
        public const string Junior = "Junior";
        public const string Mid = "Mid";
        public const string Senior = "Senior";

        /// <summary>All selectable levels, in increasing order of seniority.</summary>
        public static readonly IReadOnlyList<string> All = [Junior, Mid, Senior];

        /// <summary>Resolves a badge color for a given level. Unknown/empty values normalize safely.</summary>
        public static Color ColorFor(string? level) => level?.Trim().ToLowerInvariant() switch
        {
            "junior" => Color.Info,
            "mid" or "middle" or "mid-level" => Color.Warning,
            "senior" => Color.Success,
            _ => Color.Default
        };
    }
}
