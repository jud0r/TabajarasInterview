using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="Interview"/> to the <c>interviews</c> table.</summary>
public class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.ToTable("Interviews");
        builder.ConfigureSoftDelete();
        builder.Property(x => x.Name).HasMaxLength(255);
        builder.Property(x => x.InterviewType).HasMaxLength(15);
        builder.Property(x => x.Status).HasMaxLength(15);
        builder.Property(x => x.Score).HasPrecision(5, 2);
        builder.Property(x => x.Comments).HasColumnType("nvarchar(max)");

        builder.HasOne(x => x.CandidateApplication)
            .WithMany(x => x.Interviews)
            .HasForeignKey(x => x.CandidateApplicationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Interviewer)
            .WithMany(x => x.Interviews)
            .HasForeignKey(x => x.InterviewerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
