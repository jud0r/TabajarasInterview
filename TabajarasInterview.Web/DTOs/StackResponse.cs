namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Stack returned by the rust-api (mirrors the API's <c>StackResponse</c>
/// and the <c>stacks</c> table). Property names map to the API's snake_case
/// JSON through <see cref="Services.Api.ApiResponseParserService"/>.
/// </summary>
public sealed class StackResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Maps to <c>created_at</c>.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Maps to <c>updated_at</c> (null until the stack is edited).</summary>
    public DateTime? UpdatedAt { get; set; }
}
