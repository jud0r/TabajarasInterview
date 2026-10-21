namespace TabajarasInterview.Api.Dtos;

public sealed record CreateUserRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public sealed record UpdateUserRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

public sealed record ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}
