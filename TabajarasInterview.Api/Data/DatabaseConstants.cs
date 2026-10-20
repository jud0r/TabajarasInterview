namespace TabajarasInterview.Api.Data;

/// <summary>
/// Shared database constants used by the entity configurations.
/// </summary>
internal static class DatabaseConstants
{
    /// <summary>SQL default for UTC timestamps (replaces MySQL <c>CURRENT_TIMESTAMP</c> on a UTC server).</summary>
    public const string UtcNow = "SYSUTCDATETIME()";

    /// <summary>Case- and accent-insensitive collation, equivalent to MySQL <c>utf8mb4_0900_ai_ci</c>.</summary>
    public const string CaseAccentInsensitiveCollation = "Latin1_General_100_CI_AI";
}
