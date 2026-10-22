using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="InterviewReviewer"/> to the <c>interview_reviewers</c> table.</summary>
public class InterviewReviewerConfiguration : IEntityTypeConfiguration<InterviewReviewer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InterviewReviewer> builder)
    {
        builder.ToTable("InterviewReviewers");
        builder.ConfigureSoftDelete();
        builder.Property(x => x.ReviewScore).HasPrecision(5, 2);
        builder.Property(x => x.ReviewComments).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.InterviewId, x.ReviewerId }).IsUnique();

        builder.HasOne(x => x.Interview)
            .WithMany(x => x.InterviewReviewers)
            .HasForeignKey(x => x.InterviewId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Reviewer)
            .WithMany(x => x.InterviewReviewers)
            .HasForeignKey(x => x.ReviewerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
