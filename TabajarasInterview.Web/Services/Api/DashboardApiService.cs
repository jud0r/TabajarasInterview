using TabajarasInterview.Web.DTOs.Dashboard;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    public class DashboardApiService(
        AuthorizedHttpClientFactory authorizedFactory,
        ApiResponseParserService parser) : IDashboardApiService
    {
        private const string BaseUrl = "rust-api";

        public async Task<ApiResult<DashboardData>> GetOverviewAsync(DashboardFilter filter, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(BaseUrl);

                var query = filter.ToQueryString();
                var url = string.IsNullOrEmpty(query)
                    ? "api/dashboard/overview"
                    : $"api/dashboard/overview?{query}";

                var response = await client.GetAsync(url, ct);
                return await parser.ParseAsync<DashboardData>(response, ct);
            }
            catch (Exception ex)
            {
                // Connectivity/serialization issues shouldn't tear down the Blazor circuit.
                // Surface a failed result so the page can degrade gracefully.
                return ApiResult<DashboardData>.Fail(ex.Message);
            }
        }
    }
}
