using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// <see cref="IApplicationApiService"/> implementation backed by the rust-api.
    /// Every request is authorized and parsed through <see cref="ApiResponseParserService"/>
    /// into an <see cref="ApiResult"/>.
    /// </summary>
    public class ApplicationApiService(
        AuthorizedHttpClientFactory authorizedFactory,
        ApiResponseParserService parser) : IApplicationApiService
    {
        private const string ClientName = "rust-api";

        public async Task<ApiResult<List<ApplicationResponse>>> GetApplicationsByPositionAsync(int positionId, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.GetAsync($"api/candidate_applications/by_position/{positionId}", ct);
                return await parser.ParseAsync<List<ApplicationResponse>>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<List<ApplicationResponse>>.Fail(parser.Describe(ex));
            }
        }
    }
}
