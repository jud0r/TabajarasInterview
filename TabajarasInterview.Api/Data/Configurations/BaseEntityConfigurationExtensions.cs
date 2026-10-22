using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data.Configurations;

/// <summary>
/// Shared mapping for the columns defined by <see cref="BaseEntity"/> and <see cref="SoftDeleteEntity"/>.
/// </summary>
internal static class BaseEntityConfigurationExtensions
{
    /// <summary>
    /// Maps the key and the <c>created_at</c> / <c>updated_at</c> columns of a <see cref="BaseEntity"/>.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="builder">Entity type builder.</param>
    public static void ConfigureBase<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql(DatabaseConstants.UtcNow);
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
    }

    /// <summary>
    /// Maps the base columns plus the <c>deleted_at</c> column of a <see cref="SoftDeleteEntity"/>.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="builder">Entity type builder.</param>
    public static void ConfigureSoftDelete<T>(this EntityTypeBuilder<T> builder) where T : SoftDeleteEntity
    {
        builder.ConfigureBase();
        builder.Property(x => x.DeletedAt).HasColumnType("datetime2");
    }
}
