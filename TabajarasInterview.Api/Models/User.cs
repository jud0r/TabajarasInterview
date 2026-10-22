namespace TabajarasInterview.Api.Models;

/// <summary>
/// Application user (interviewer, reviewer or position creator). Maps to the <c>users</c> table.
/// </summary>
public class User : SoftDeleteEntity
{
    /// <summary>First name.</summary>
    public required string FirstName { get; set; }

    /// <summary>Last name.</summary>
    public required string LastName { get; set; }

    /// <summary>Unique login e-mail.</summary>
    public required string Email { get; set; }

    /// <summary>bcrypt password hash.</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Reviews assigned to this user.</summary>
    public ICollection<InterviewReviewer> InterviewReviewers { get; set; } = [];

    /// <summary>Interviews conducted by this user.</summary>
    public ICollection<Interview> Interviews { get; set; } = [];

    /// <summary>Positions created by this user.</summary>
    public ICollection<Position> Positions { get; set; } = [];

    /// <summary>Refresh tokens issued to this user.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
