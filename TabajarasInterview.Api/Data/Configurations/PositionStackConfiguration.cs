using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="PositionStack"/> to the <c>position_stacks</c> table.</summary>
public class PositionStackConfiguration : IEntityTypeConfiguration<PositionStack>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PositionStack> builder)
    {
        builder.ToTable("PositionStacks");
        builder.HasKey(x => new { x.PositionId, x.StackId });
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql(DatabaseConstants.UtcNow);
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        builder.Property(x => x.DeletedAt).HasColumnType("datetime2");
    }
}
