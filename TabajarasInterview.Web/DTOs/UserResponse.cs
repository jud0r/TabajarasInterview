namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Authenticated user returned by the rust-api (matches the API's <c>UserResponse</c>).
/// </summary>
public sealed class UserResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
