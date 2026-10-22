using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="User"/> to the <c>users</c> table.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.ConfigureSoftDelete();
        builder.Property(x => x.FirstName).HasMaxLength(255);
        builder.Property(x => x.LastName).HasMaxLength(255);
        builder.Property(x => x.Email).HasMaxLength(255).UseCollation(DatabaseConstants.CaseAccentInsensitiveCollation);
        builder.Property(x => x.PasswordHash).HasMaxLength(255);
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
