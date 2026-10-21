using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Dtos;

/// <summary>Rust <c>StackResponse</c>.</summary>
public sealed record StackResponse(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static StackResponse From(Stack stack) =>
        new(stack.Id, stack.Name, stack.Description, stack.CreatedAt, stack.UpdatedAt);
}
