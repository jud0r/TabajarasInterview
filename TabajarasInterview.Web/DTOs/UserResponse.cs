namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Authenticated user returned by the rust-api (matches the API's <c>UserResponse</c>).
/// </summary>
public sealed record UserResponse(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
