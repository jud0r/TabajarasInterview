using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>Mapping of <see cref="Question"/> to the <c>questions</c> table.</summary>
public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");
        builder.ConfigureSoftDelete();
        builder.Property(x => x.Text).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AcceptableAnswer).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Level).HasMaxLength(10);

        builder.HasOne(x => x.Stack)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.StackId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
