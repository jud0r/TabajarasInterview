using System.Text.RegularExpressions;

namespace TabajarasInterview.Api.Common;

/// <summary>
/// Reproduces the Rust <c>validator</c> rules used by the handlers. Failures become 400 with a plain-text
/// body formatted as <c>field: message</c> (one line per failing field).
/// </summary>
public sealed partial class RequestValidator
{
    private readonly List<string> _errors = [];

    public RequestValidator MinLength(string field, string? value, int min, string message)
    {
        // validator counts Unicode scalar values, not UTF-16 units.
        if (value is not null && CountChars(value) < min)
        {
            _errors.Add($"{field}: {message}");
        }

        return this;
    }

    public RequestValidator Email(string field, string value, string message)
    {
        if (!EmailRegex().IsMatch(value) || value.Length > 254)
        {
            _errors.Add($"{field}: {message}");
        }

        return this;
    }

    /// <summary>Throws <see cref="ApiException"/> (400) if any rule failed.</summary>
    public void ThrowIfInvalid()
    {
        if (_errors.Count > 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, string.Join("\n", _errors));
        }
    }

    private static int CountChars(string value)
    {
        var count = 0;
        foreach (var _ in value.EnumerateRunes())
        {
            count++;
        }

        return count;
    }

    [GeneratedRegex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
