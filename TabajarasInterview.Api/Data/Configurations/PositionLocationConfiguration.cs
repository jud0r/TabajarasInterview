using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="PositionLocation"/> to the <c>position_locations</c> table.</summary>
public class PositionLocationConfiguration : IEntityTypeConfiguration<PositionLocation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PositionLocation> builder)
    {
        builder.ToTable("PositionLocations");
        builder.HasKey(x => new { x.PositionId, x.LocationId });
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql(DatabaseConstants.UtcNow);
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        builder.Property(x => x.DeletedAt).HasColumnType("datetime2");
    }
}
