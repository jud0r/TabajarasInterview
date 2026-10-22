using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="Position"/> to the <c>positions</c> table.</summary>
public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        builder.ConfigureSoftDelete();
        builder.Property(x => x.Title).HasMaxLength(500);
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).HasMaxLength(15);
        builder.Property(x => x.WorkModel).HasMaxLength(15);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.Positions)
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(x => x.Locations)
            .WithMany(x => x.Positions)
            .UsingEntity<PositionLocation>(
                right => right.HasOne(x => x.Location)
                    .WithMany(x => x.PositionLocations)
                    .HasForeignKey(x => x.LocationId)
                    .OnDelete(DeleteBehavior.NoAction),
                left => left.HasOne(x => x.Position)
                    .WithMany(x => x.PositionLocations)
                    .HasForeignKey(x => x.PositionId)
                    .OnDelete(DeleteBehavior.NoAction));

        builder.HasMany(x => x.Stacks)
            .WithMany(x => x.Positions)
            .UsingEntity<PositionStack>(
                right => right.HasOne(x => x.Stack)
                    .WithMany(x => x.PositionStacks)
                    .HasForeignKey(x => x.StackId)
                    .OnDelete(DeleteBehavior.NoAction),
                left => left.HasOne(x => x.Position)
                    .WithMany(x => x.PositionStacks)
                    .HasForeignKey(x => x.PositionId)
                    .OnDelete(DeleteBehavior.NoAction));
    }
}
