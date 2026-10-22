using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="Stack"/> to the <c>stacks</c> table.</summary>
public class StackConfiguration : IEntityTypeConfiguration<Stack>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Stack> builder)
    {
        builder.ToTable("Stacks");
        builder.ConfigureSoftDelete();
        builder.Property(x => x.Name).HasMaxLength(255).UseCollation(DatabaseConstants.CaseAccentInsensitiveCollation);
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
