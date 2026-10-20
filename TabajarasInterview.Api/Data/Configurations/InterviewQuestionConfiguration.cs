using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="InterviewQuestion"/> to the <c>interview_questions</c> table.</summary>
public class InterviewQuestionConfiguration : IEntityTypeConfiguration<InterviewQuestion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InterviewQuestion> builder)
    {
        builder.ToTable("InterviewQuestions");
        builder.ConfigureSoftDelete();
        builder.Property(x => x.Score).HasPrecision(5, 2);

        builder.HasIndex(x => new { x.InterviewId, x.QuestionOrder }).IsUnique();
        builder.HasIndex(x => new { x.InterviewId, x.QuestionId }).IsUnique();

        builder.HasOne(x => x.Interview)
            .WithMany(x => x.InterviewQuestions)
            .HasForeignKey(x => x.InterviewId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Question)
            .WithMany(x => x.InterviewQuestions)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
