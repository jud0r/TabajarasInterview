namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Position returned by the API (mirrors the API's <c>PositionResponse</c>
/// and the <c>positions</c> table). Property names map to the API's camelCase
/// JSON through <see cref="Services.Api.ApiResponseParserService"/>.
/// </summary>
public sealed class PositionResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Lifecycle status. The API's <c>PositionStatus</c> enum serializes as
    /// camelCase on the wire (draft, open, onHold, closed, cancelled) regardless
    /// of the PascalCase strings stored in the database column.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Work model (remote, hybrid, onsite). Maps to <c>workModel</c>.</summary>
    public string? WorkModel { get; set; }

    /// <summary>Id of the user (a <c>users</c> row) who created the position. Maps to <c>createdBy</c>.</summary>
    public int CreatedBy { get; set; }

    /// <summary>Maps to <c>createdAt</c>.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Maps to <c>updatedAt</c> (null until the position is edited).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Tech stacks required for the position (embedded by the API's list/detail endpoints).</summary>
    public List<StackResponse> Stacks { get; set; } = [];
}
