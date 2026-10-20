using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="RefreshToken"/> to the <c>refresh_tokens</c> table.</summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(255);
        builder.Property(x => x.ExpiresAt).HasColumnType("datetime2");
        builder.Property(x => x.RevokedAt).HasColumnType("datetime2");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql(DatabaseConstants.UtcNow);

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
