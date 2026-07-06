namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Position returned by the rust-api (mirrors the API's <c>PositionResponse</c>
/// and the <c>positions</c> table). Property names map to the API's snake_case
/// JSON through <see cref="Services.Api.ApiResponseParserService"/>.
/// </summary>
public sealed class PositionResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Lifecycle status. The rust-api's <c>PositionStatus</c> enum serializes as
    /// snake_case on the wire (draft, open, on_hold, closed, cancelled) regardless
    /// of the PascalCase strings stored in the database column.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Id of the user (a <c>users</c> row) who created the position. Maps to <c>created_by</c>.</summary>
    public int CreatedBy { get; set; }

    /// <summary>Maps to <c>created_at</c>.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Maps to <c>updated_at</c> (null until the position is edited).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Tech stacks required for the position (embedded by the API's list/detail endpoints).</summary>
    public List<StackResponse> Stacks { get; set; } = [];
}
