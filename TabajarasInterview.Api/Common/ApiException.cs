namespace TabajarasInterview.Api.Common;

/// <summary>
/// Mirrors the Rust <c>(StatusCode, &amp;'static str)</c> error tuple: a status code with a plain-text body.
/// </summary>
public sealed class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
