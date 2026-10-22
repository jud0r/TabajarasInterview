namespace TabajarasInterview.Api.Models;

/// <summary>
/// Persisted refresh token (stored hashed). Maps to the <c>refresh_tokens</c> table.
/// </summary>
public class RefreshToken
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Id of the owning <see cref="User"/>.</summary>
    public int UserId { get; set; }

    /// <summary>Hash of the refresh token.</summary>
    public required string TokenHash { get; set; }

    /// <summary>Expiration timestamp (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Revocation timestamp (UTC), if revoked.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Owning user.</summary>
    public User? User { get; set; }
}
