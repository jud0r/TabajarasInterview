using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Data;

/// <summary>
/// Entity Framework Core context for the interview database (SQL Server).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Users.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Candidates.</summary>
    public DbSet<Candidate> Candidates => Set<Candidate>();

    /// <summary>Positions.</summary>
    public DbSet<Position> Positions => Set<Position>();

    /// <summary>Locations.</summary>
    public DbSet<Location> Locations => Set<Location>();

    /// <summary>Position/location join rows.</summary>
    public DbSet<PositionLocation> PositionLocations => Set<PositionLocation>();

    /// <summary>Stacks.</summary>
    public DbSet<Stack> Stacks => Set<Stack>();

    /// <summary>Position/stack join rows.</summary>
    public DbSet<PositionStack> PositionStacks => Set<PositionStack>();

    /// <summary>Questions.</summary>
    public DbSet<Question> Questions => Set<Question>();

    /// <summary>Candidate applications.</summary>
    public DbSet<CandidateApplication> CandidateApplications => Set<CandidateApplication>();

    /// <summary>Interviews.</summary>
    public DbSet<Interview> Interviews => Set<Interview>();

    /// <summary>Interview questions.</summary>
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();

    /// <summary>Interview reviewers.</summary>
    public DbSet<InterviewReviewer> InterviewReviewers => Set<InterviewReviewer>();

    /// <summary>Refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Hide soft-deleted rows. Use IgnoreQueryFilters() to include them.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => t.BaseType is null && typeof(ISoftDeleteEntity).IsAssignableFrom(t.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var deletedAt = Expression.Property(parameter, nameof(ISoftDeleteEntity.DeletedAt));
            var isActive = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTime?)));
            entityType.SetQueryFilter(Expression.Lambda(isActive, parameter));
        }
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        TouchUpdatedAt();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        TouchUpdatedAt();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Replaces MySQL's <c>ON UPDATE CURRENT_TIMESTAMP</c> by stamping <c>UpdatedAt</c> on modified rows.
    /// </summary>
    private void TouchUpdatedAt()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
        {
            switch (entry.Entity)
            {
                case BaseEntity baseEntity when !entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified:
                    baseEntity.UpdatedAt = now;
                    break;
                case PositionLocation positionLocation when !entry.Property(nameof(PositionLocation.UpdatedAt)).IsModified:
                    positionLocation.UpdatedAt = now;
                    break;
                case PositionStack positionStack when !entry.Property(nameof(PositionStack.UpdatedAt)).IsModified:
                    positionStack.UpdatedAt = now;
                    break;
            }
        }
    }
}
