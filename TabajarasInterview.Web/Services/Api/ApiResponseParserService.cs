using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    public class ApiResponseParserService
    {
        public async Task<ApiResult<T>> ParseAsync<T>(HttpResponseMessage httpResponse, CancellationToken ct = default)
        {
            if (httpResponse.IsSuccessStatusCode)
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return ApiResult<T>.Ok(default!);
                }

                var data = await httpResponse.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return ApiResult<T>.Ok(data!);
            }

            return await ParseErrorAsync<ApiResult<T>>(httpResponse, ct);
        }

        public async Task<ApiResult> ParseAsync(HttpResponseMessage httpResponse, CancellationToken ct = default)
        {
            if (httpResponse.IsSuccessStatusCode)
                return ApiResult.Ok();

            return await ParseErrorAsync<ApiResult>(httpResponse, ct);
        }

        private static async Task<T> ParseErrorAsync<T>(HttpResponseMessage httpResponse, CancellationToken ct) where T : ApiResult, new()
        {
            try
            {
                var apiError = await httpResponse.Content.ReadFromJsonAsync<ApiError>(cancellationToken: ct);

                if (apiError?.Errors is not null)
                    return new T { Success = false, ValidationErrors = apiError.Errors };

                return new T { Success = false, ErrorCode = apiError?.Code, ErrorMessage = $"{apiError?.Code}: {apiError?.Error}" };
            }
            catch
            {
                return new T
                {
                    Success = false,
                    ErrorMessage = $"HTTP {(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}"
                };
            }
        }
    }
}
