namespace TabajarasInterview.Web.Components.Shared;

/// <summary>
/// Work models supported for a position (positions.work_model).
/// Wire values must match the rust-api / database column; adjust if they differ.
/// </summary>
public static class WorkModel
{
    public const string Remote = "remote";
    public const string Hybrid = "hybrid";
    public const string OnSite = "onsite";

    public static readonly IReadOnlyList<string> All = [Remote, Hybrid, OnSite];

    public static string Label(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        Remote => "Remote",
        Hybrid => "Hybrid",
        OnSite => "On-site",
        _ => string.IsNullOrWhiteSpace(value) ? "-" : value!
    };
}
