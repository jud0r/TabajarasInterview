using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace TabajarasInterview.Api.Common;

/// <summary>
/// Endpoint filter reproducing the Rust error handling: <see cref="ApiException"/> becomes a plain-text
/// response, and database failures are logged ("DB ERROR") and returned as 500 "DB error".
/// </summary>
public sealed class ApiErrorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ApiException e)
        {
            return PlainText(e.StatusCode, e.Message);
        }
        catch (Exception e) when (e is DbException or DbUpdateException)
        {
            context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger<ApiErrorFilter>()
                .LogError("DB ERROR: {Message}", e.Message);
            return PlainText(StatusCodes.Status500InternalServerError, "DB error");
        }
    }

    private static IResult PlainText(int status, string message) =>
        Results.Text(message, "text/plain; charset=utf-8", statusCode: status);
}
