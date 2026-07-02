using MudBlazor;
using TabajarasInterview.Web.DTOs.Candidate;

namespace TabajarasInterview.Web.Components.Shared
{
    /// <summary>
    /// Central, defensive mapping from raw status strings (application, interview
    /// or position) to a MudBlazor <see cref="Color"/> and icon. Unknown or slightly
    /// different values normalize safely to a neutral badge, so the UI never throws
    /// on an unexpected status coming from the API.
    /// </summary>
    public static class StatusVisuals
    {
        /// <summary>Resolves the color + icon pair for any status value.</summary>
        public static (Color Color, string Icon) For(string? status) =>
            CandidateStatusKind.Normalize(status) switch
            {
                "hired" or "approved" or "passed" => (Color.Success, Icons.Material.Filled.CheckCircle),
                "completed" or "reviewed" or "done" => (Color.Success, Icons.Material.Filled.TaskAlt),
                "open" => (Color.Success, Icons.Material.Filled.LockOpen),
                "offer" or "offerextended" => (Color.Tertiary, Icons.Material.Filled.LocalOffer),
                "interviewing" or "inprogress" => (Color.Primary, Icons.Material.Filled.HourglassTop),
                "applied" or "screening" or "scheduled" or "pending" => (Color.Info, Icons.Material.Filled.Schedule),
                "onhold" or "rescheduled" => (Color.Warning, Icons.Material.Filled.PauseCircle),
                "draft" => (Color.Default, Icons.Material.Filled.EditNote),
                "rejected" or "declined" or "noshow" or "cancelled" or "canceled" => (Color.Error, Icons.Material.Filled.Cancel),
                "withdrawn" or "closed" => (Color.Default, Icons.Material.Filled.DoNotDisturbOn),
                "" => (Color.Default, Icons.Material.Filled.HelpOutline),
                _ => (Color.Default, Icons.Material.Filled.Label)
            };

        /// <summary>Convenience accessor for just the color.</summary>
        public static Color ColorFor(string? status) => For(status).Color;

        /// <summary>Convenience accessor for just the icon.</summary>
        public static string IconFor(string? status) => For(status).Icon;

        /// <summary>Human-friendly label for an empty/unknown status.</summary>
        public static string Label(string? status) =>
            string.IsNullOrWhiteSpace(status) ? "Unknown" : status!;

        /// <summary>Maps a 0-10 score onto a semantic color for badges/charts.</summary>
        public static Color ScoreColor(double? score) => score switch
        {
            null => Color.Default,
            >= 7.5 => Color.Success,
            >= 5 => Color.Warning,
            _ => Color.Error
        };
    }
}
