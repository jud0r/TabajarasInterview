using System.Globalization;

namespace TabajarasInterview.Web.DTOs.Dashboard;

/// <summary>
/// Filter applied to the recruitment dashboard. All members are optional so the
/// dashboard can render an "all time / all positions" overview by default.
/// </summary>
public sealed class DashboardFilter
{
    /// <summary>Inclusive lower bound for application/interview dates.</summary>
    public DateTime? From { get; set; }

    /// <summary>Inclusive upper bound for application/interview dates.</summary>
    public DateTime? To { get; set; }

    /// <summary>Restrict the overview to a single position (<c>positions.id</c>).</summary>
    public int? PositionId { get; set; }

    /// <summary>Restrict interview analytics to a single interview type (e.g. "Technical").</summary>
    public string? InterviewType { get; set; }

    /// <summary>
    /// Builds the query-string (without leading '?') used to call the aggregated
    /// dashboard endpoint. Returns an empty string when no filter is set.
    /// </summary>
    public string ToQueryString()
    {
        var parts = new List<string>(4);

        if (From is { } from)
            parts.Add($"from={Uri.EscapeDataString(from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");

        if (To is { } to)
            parts.Add($"to={Uri.EscapeDataString(to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");

        if (PositionId is { } positionId)
            parts.Add($"position_id={positionId}");

        if (!string.IsNullOrWhiteSpace(InterviewType))
            parts.Add($"interview_type={Uri.EscapeDataString(InterviewType)}");

        return string.Join('&', parts);
    }
}
