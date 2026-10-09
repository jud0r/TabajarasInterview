using Xunit;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TabajarasInterview.Web.Services.Api;

namespace TabajarasInterview.Web.Tests;

public class ApiResponseParserServiceTests
{
    private sealed class Item
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
    }

    private readonly ApiResponseParserService _parser = new(NullLogger<ApiResponseParserService>.Instance);

    private static HttpResponseMessage Response(HttpStatusCode status, string? json = null)
    {
        var response = new HttpResponseMessage(status);
        if (json is not null)
        {
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return response;
    }

    [Fact]
    public async Task ParseAsync_Success_MapsSnakeCaseJson()
    {
        var result = await _parser.ParseAsync<Item>(Response(HttpStatusCode.OK, """{"id":7,"first_name":"Ada"}"""));

        Assert.True(result.Success);
        Assert.Equal(7, result.Data!.Id);
        Assert.Equal("Ada", result.Data.FirstName);
    }

    [Fact]
    public async Task ParseAsync_NoContent_ReturnsSuccessWithoutData()
    {
        var result = await _parser.ParseAsync<Item>(Response(HttpStatusCode.NoContent));

        Assert.True(result.Success);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task ParseAsync_EmptyBody_ReturnsSuccessWithoutData()
    {
        var result = await _parser.ParseAsync<Item>(Response(HttpStatusCode.OK, ""));

        Assert.True(result.Success);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task ParseAsync_MalformedJson_ReturnsFailure()
    {
        var result = await _parser.ParseAsync<Item>(Response(HttpStatusCode.OK, "{not json"));

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public async Task ParseAsync_ValidationErrors_AreReturned()
    {
        var json = """{"errors":{"email":["is invalid"]}}""";

        var result = await _parser.ParseAsync<Item>(Response(HttpStatusCode.UnprocessableEntity, json));

        Assert.False(result.Success);
        Assert.Equal(["is invalid"], result.ValidationErrors!["email"]);
    }

    [Fact]
    public async Task ParseAsync_ErrorWithCodeAndMessage_CombinesBoth()
    {
        var json = """{"code":"not_found","error":"Missing"}""";

        var result = await _parser.ParseAsync<Item>(Response(HttpStatusCode.NotFound, json));

        Assert.False(result.Success);
        Assert.Equal("not_found", result.ErrorCode);
        Assert.Equal("not_found: Missing", result.ErrorMessage);
    }

    [Fact]
    public async Task ParseAsync_ErrorWithEmptyJson_FallsBackToHttpStatus()
    {
        var result = await _parser.ParseAsync<Item>(Response(HttpStatusCode.InternalServerError, "{}"));

        Assert.False(result.Success);
        Assert.StartsWith("HTTP 500", result.ErrorMessage);
    }

    [Fact]
    public async Task ParseAsync_ErrorWithNonJsonBody_FallsBackToHttpStatus()
    {
        var result = await _parser.ParseAsync(Response(HttpStatusCode.BadGateway, "<html>bad gateway</html>"));

        Assert.False(result.Success);
        Assert.StartsWith("HTTP 502", result.ErrorMessage);
    }

    [Fact]
    public async Task ParseAsync_NonGeneric_SuccessReturnsOk()
    {
        var result = await _parser.ParseAsync(Response(HttpStatusCode.OK));

        Assert.True(result.Success);
    }

    [Fact]
    public void Describe_DoesNotLeakExceptionMessage()
    {
        var message = _parser.Describe(new InvalidOperationException("secret connection string"));

        Assert.DoesNotContain("secret", message);
    }

    [Fact]
    public void Describe_HttpRequestException_ReturnsConnectivityMessage()
    {
        var message = _parser.Describe(new HttpRequestException("boom"));

        Assert.Contains("reach the server", message);
    }
}
