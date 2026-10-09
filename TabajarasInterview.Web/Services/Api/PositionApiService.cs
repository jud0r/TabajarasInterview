using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// <see cref="IPositionApiService"/> implementation backed by the rust-api.
    /// Every request is authorized (the position endpoints require a valid JWT) and
    /// parsed through <see cref="ApiResponseParserService"/> into an <see cref="ApiResult"/>.
    /// </summary>
    public class PositionApiService(
        AuthorizedHttpClientFactory authorizedFactory,
        ApiResponseParserService parser) : IPositionApiService
    {
        private const string ClientName = "rust-api";

        public async Task<ApiResult<List<PositionResponse>>> GetPositionsAsync(CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.GetAsync("api/positions/get_all", ct);
                return await parser.ParseAsync<List<PositionResponse>>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<List<PositionResponse>>.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult<PositionResponse>> GetPositionByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.GetAsync($"api/positions/get/{id}", ct);
                return await parser.ParseAsync<PositionResponse>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<PositionResponse>.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult<PositionResponse>> CreatePositionAsync(CreatePositionRequest request, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);
                var payload = new
                {
                    title = request.Title,
                    description = request.Description,
                    status = request.Status,
                    work_model = request.WorkModel
                };

                var response = await client.PostAsJsonAsync("api/positions/create", payload, ct);
                return await parser.ParseAsync<PositionResponse>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<PositionResponse>.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult<PositionResponse>> UpdatePositionAsync(int id, UpdatePositionRequest request, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);
                var payload = new
                {
                    title = request.Title,
                    description = request.Description,
                    status = request.Status,
                    work_model = request.WorkModel
                };

                var response = await client.PutAsJsonAsync($"api/positions/update/{id}", payload, ct);
                return await parser.ParseAsync<PositionResponse>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<PositionResponse>.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult> DeletePositionAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.DeleteAsync($"api/positions/delete/{id}", ct);
                return await parser.ParseAsync(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult<PositionStatsResponse>> GetPositionStatsAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var applicationsResponse = await client.GetAsync($"api/candidate_applications/by_position/{id}", ct);
                var applicationsResult = await parser.ParseAsync<List<RawApplicationEntry>>(applicationsResponse, ct);
                if (applicationsResult is not { Success: true, Data: not null })
                {
                    return ApiResult<PositionStatsResponse>.Fail(applicationsResult.ErrorMessage ?? "Could not load application statistics.");
                }

                var applicationIds = applicationsResult.Data.Select(a => a.Id).ToHashSet();

                // The rust-api has no "interviews by position" endpoint, so the full
                // interview list is fetched and filtered client-side by application id.
                var interviewsResponse = await client.GetAsync("api/interviews/get_all", ct);
                var interviewsResult = await parser.ParseAsync<List<RawInterviewEntry>>(interviewsResponse, ct);
                if (interviewsResult is not { Success: true, Data: not null })
                {
                    return ApiResult<PositionStatsResponse>.Fail(interviewsResult.ErrorMessage ?? "Could not load interview statistics.");
                }

                var interviewCount = interviewsResult.Data.Count(i => applicationIds.Contains(i.CandidateApplicationId));

                return ApiResult<PositionStatsResponse>.Ok(new PositionStatsResponse
                {
                    ApplicationCount = applicationIds.Count,
                    InterviewCount = interviewCount
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<PositionStatsResponse>.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult<List<StackResponse>>> AssignStackAsync(int positionId, int stackId, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.PostAsync($"api/positions/get/{positionId}/stacks/{stackId}", null, ct);
                return await parser.ParseAsync<List<StackResponse>>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<List<StackResponse>>.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult<List<StackResponse>>> RemoveStackAsync(int positionId, int stackId, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.DeleteAsync($"api/positions/get/{positionId}/stacks/{stackId}", ct);
                return await parser.ParseAsync<List<StackResponse>>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<List<StackResponse>>.Fail(parser.Describe(ex));
            }
        }

        /// <summary>Minimal shape used only to count applications returned by the by-position endpoint.</summary>
        private sealed class RawApplicationEntry
        {
            public int Id { get; set; }
        }

        /// <summary>Minimal shape used only to filter interviews by their parent application id.</summary>
        private sealed class RawInterviewEntry
        {
            public int Id { get; set; }
            public int CandidateApplicationId { get; set; }
        }
    }
}
