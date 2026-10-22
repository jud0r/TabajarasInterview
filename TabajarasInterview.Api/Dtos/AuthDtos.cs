namespace TabajarasInterview.Api.Dtos;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, int ExpiresIn, string RefreshToken, UserResponse User);

public sealed record RefreshRequest(string RefreshToken);

public sealed record RefreshResponse(string AccessToken, int ExpiresIn, string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);
