using MudBlazor;

namespace TabajarasInterview.Web.Components.Shared
{
    /// <summary>
    /// Central definition of the position lifecycle statuses supported by the UI,
    /// plus a defensive mapping to display labels. Mirrors the pattern used by
    /// <see cref="QuestionLevels"/>.
    /// </summary>
    /// <remarks>
    /// Wire values are snake_case to match the rust-api's <c>PositionStatus</c>
    /// enum (<c>#[serde(rename_all = "snake_case")]</c>): requests/responses use
    /// <c>draft</c>, <c>open</c>, <c>on_hold</c>, <c>closed</c>, <c>cancelled</c>.
    /// Badge colors/icons are resolved through <see cref="StatusVisuals"/>, whose
    /// normalization already strips underscores so <c>on_hold</c> matches its
    /// existing "onhold" case with no changes needed there.
    /// </remarks>
    public static class PositionStatuses
    {
        public const string Draft = "draft";
        public const string Open = "open";
        public const string OnHold = "on_hold";
        public const string Closed = "closed";
        public const string Cancelled = "cancelled";

        /// <summary>All selectable statuses, in typical lifecycle order.</summary>
        public static readonly IReadOnlyList<string> All = [Draft, Open, OnHold, Closed, Cancelled];

        /// <summary>Human-friendly label for a wire-format status value (e.g. "on_hold" -&gt; "On Hold").</summary>
        public static string Label(string? status) => status?.Trim().ToLowerInvariant() switch
        {
            Draft => "Draft",
            Open => "Open",
            OnHold => "On Hold",
            Closed => "Closed",
            Cancelled => "Cancelled",
            _ => string.IsNullOrWhiteSpace(status) ? "Unknown" : status!
        };

        /// <summary>Resolves a badge color for a given status. Unknown/empty values normalize safely.</summary>
        public static Color ColorFor(string? status) => StatusVisuals.ColorFor(status);
    }
}
