using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="CandidateApplication"/> to the <c>candidate_applications</c> table.</summary>
public class CandidateApplicationConfiguration : IEntityTypeConfiguration<CandidateApplication>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CandidateApplication> builder)
    {
        builder.ToTable("CandidateApplications");
        builder.ConfigureSoftDelete();
        builder.Property(x => x.Status).HasMaxLength(15);
        builder.Property(x => x.StartedAt).HasColumnType("datetime2").HasDefaultValueSql(DatabaseConstants.UtcNow);
        builder.Property(x => x.FinishedAt).HasColumnType("datetime2");
        builder.Property(x => x.FinalScore).HasPrecision(5, 2);
        builder.Property(x => x.FinalComments).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.CandidateId, x.PositionId }).IsUnique();

        builder.HasOne(x => x.Candidate)
            .WithMany(x => x.CandidateApplications)
            .HasForeignKey(x => x.CandidateId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Position)
            .WithMany(x => x.CandidateApplications)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
