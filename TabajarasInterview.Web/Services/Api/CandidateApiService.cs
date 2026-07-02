using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.DTOs.Candidate;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// <see cref="ICandidateApiService"/> implementation backed by the rust-api.
    /// Every request is authorized (the candidate endpoints require a valid JWT) and
    /// parsed through <see cref="ApiResponseParserService"/> into an <see cref="ApiResult"/>.
    /// </summary>
    public class CandidateApiService(
        AuthorizedHttpClientFactory authorizedFactory,
        ApiResponseParserService parser) : ICandidateApiService
    {
        private const string ClientName = "rust-api";

        public async Task<ApiResult<List<CandidateResponse>>> GetCandidatesAsync(CancellationToken ct = default)
        {
            var client = await authorizedFactory.CreateClientAsync(ClientName);

            var response = await client.GetAsync("api/candidates/get_all", ct);
            return await parser.ParseAsync<List<CandidateResponse>>(response, ct);
        }

        public async Task<ApiResult<CandidateResponse>> GetCandidateByIdAsync(int id, CancellationToken ct = default)
        {
            var client = await authorizedFactory.CreateClientAsync(ClientName);

            var response = await client.GetAsync($"api/candidates/get/{id}", ct);
            return await parser.ParseAsync<CandidateResponse>(response, ct);
        }

        public async Task<ApiResult<CandidateResponse>> CreateCandidateAsync(CandidateRequest request, CancellationToken ct = default)
        {
            var client = await authorizedFactory.CreateClientAsync(ClientName);
            var payload = new
            {
                first_name = request.FirstName,
                last_name = request.LastName,
                email = request.Email,
                phone = request.Phone
            };

            var response = await client.PostAsJsonAsync("api/candidates/create", payload, ct);
            return await parser.ParseAsync<CandidateResponse>(response, ct);
        }

        public async Task<ApiResult<CandidateResponse>> UpdateCandidateAsync(int id, CandidateRequest request, CancellationToken ct = default)
        {
            var client = await authorizedFactory.CreateClientAsync(ClientName);
            // The API's UpdateCandidateRequest accepts only these fields; email is immutable.
            var payload = new
            {
                first_name = request.FirstName,
                last_name = request.LastName,
                phone = request.Phone,
                email= request.Email
            };

            var response = await client.PutAsJsonAsync($"api/candidates/update/{id}", payload, ct);
            return await parser.ParseAsync<CandidateResponse>(response, ct);
        }

        public async Task<ApiResult> DeleteCandidateAsync(int id, CancellationToken ct = default)
        {
            var client = await authorizedFactory.CreateClientAsync(ClientName);

            var response = await client.DeleteAsync($"api/candidates/delete/{id}", ct);
            return await parser.ParseAsync(response, ct);
        }

        public async Task<ApiResult<List<CandidateApplicationResponse>>> GetCandidatePositionsAsync(int candidateId, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                // Backed by get_candidate_positions(candidate_id) on the API side.
                var response = await client.GetAsync($"api/candidates/{candidateId}/positions", ct);
                return await parser.ParseAsync<List<CandidateApplicationResponse>>(response, ct);
            }
            catch (Exception ex)
            {
                // Connectivity/serialization issues shouldn't tear down the Blazor circuit;
                // surface a failed result so the page can fall back to sample data.
                return ApiResult<List<CandidateApplicationResponse>>.Fail(ex.Message);
            }
        }

        public async Task<ApiResult<List<TimelineEvent>>> GetCandidateTimelineAsync(int candidateId, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.GetAsync($"api/candidates/{candidateId}/timeline", ct);
                return await parser.ParseAsync<List<TimelineEvent>>(response, ct);
            }
            catch (Exception ex)
            {
                return ApiResult<List<TimelineEvent>>.Fail(ex.Message);
            }
        }
    }
}
