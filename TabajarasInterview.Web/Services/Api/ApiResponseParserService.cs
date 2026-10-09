using System.Text.Json;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    public class ApiResponseParserService(ILogger<ApiResponseParserService> logger)
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public async Task<ApiResult<T>> ParseAsync<T>(HttpResponseMessage httpResponse, CancellationToken ct = default)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                return await ParseErrorAsync<ApiResult<T>>(httpResponse, ct);
            }

            if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return ApiResult<T>.Ok(default!);
            }

            try
            {
                var body = await httpResponse.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(body))
                {
                    return ApiResult<T>.Ok(default!);
                }

                var data = JsonSerializer.Deserialize<T>(body, JsonOptions);
                return ApiResult<T>.Ok(data!);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Could not deserialize the {Type} response from {Uri}.",
                    typeof(T).Name, httpResponse.RequestMessage?.RequestUri);
                return ApiResult<T>.Fail("The server returned an unexpected response.");
            }
        }

        public async Task<ApiResult> ParseAsync(HttpResponseMessage httpResponse, CancellationToken ct = default)
        {
            if (httpResponse.IsSuccessStatusCode)
                return ApiResult.Ok();

            return await ParseErrorAsync<ApiResult>(httpResponse, ct);
        }

        /// <summary>
        /// Logs an exception raised while calling the API and returns a user-safe message
        /// (the raw exception message is never surfaced to the UI).
        /// </summary>
        public string Describe(Exception ex)
        {
            logger.LogError(ex, "API call failed.");

            return ex is HttpRequestException
                ? "Could not reach the server. Please try again."
                : "An unexpected error occurred. Please try again.";
        }

        private async Task<T> ParseErrorAsync<T>(HttpResponseMessage httpResponse, CancellationToken ct) where T : ApiResult, new()
        {
            try
            {
                var apiError = await httpResponse.Content.ReadFromJsonAsync<ApiError>(JsonOptions, cancellationToken: ct);

                if (apiError?.Errors is not null)
                    return new T { Success = false, ValidationErrors = apiError.Errors };

                if (apiError is not null && (!string.IsNullOrWhiteSpace(apiError.Code) || !string.IsNullOrWhiteSpace(apiError.Error)))
                {
                    var message = string.IsNullOrWhiteSpace(apiError.Code)
                        ? apiError.Error
                        : string.IsNullOrWhiteSpace(apiError.Error)
                            ? apiError.Code
                            : $"{apiError.Code}: {apiError.Error}";

                    return new T { Success = false, ErrorCode = apiError.Code, ErrorMessage = message };
                }
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
            {
                logger.LogWarning(ex, "Could not parse the error body of HTTP {StatusCode} from {Uri}.",
                    (int)httpResponse.StatusCode, httpResponse.RequestMessage?.RequestUri);
            }

            return new T
            {
                Success = false,
                ErrorMessage = $"HTTP {(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}"
            };
        }
    }
}
