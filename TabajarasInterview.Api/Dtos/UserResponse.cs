using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Dtos;

/// <summary>Rust <c>UserResponse</c> (users.rs). Serialized with camelCase names.</summary>
public sealed record UserResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.CreatedAt, user.UpdatedAt);
}
