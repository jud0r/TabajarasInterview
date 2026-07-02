using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// <see cref="IStackApiService"/> implementation backed by the rust-api.
    /// Every request is authorized (the stack endpoints require a valid JWT) and
    /// parsed through <see cref="ApiResponseParserService"/> into an <see cref="ApiResult"/>.
    /// </summary>
    public class StackApiService(
        AuthorizedHttpClientFactory authorizedFactory,
        ApiResponseParserService parser) : IStackApiService
    {
        private const string ClientName = "rust-api";

        public async Task<ApiResult<List<StackResponse>>> GetStacksAsync(CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.GetAsync("api/stacks/get_all", ct);
                return await parser.ParseAsync<List<StackResponse>>(response, ct);
            }
            catch (Exception ex)
            {
                return ApiResult<List<StackResponse>>.Fail(ex.Message);
            }
        }

        public async Task<ApiResult<StackResponse>> GetStackByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.GetAsync($"api/stacks/get/{id}", ct);
                return await parser.ParseAsync<StackResponse>(response, ct);
            }
            catch (Exception ex)
            {
                return ApiResult<StackResponse>.Fail(ex.Message);
            }
        }

        public async Task<ApiResult<StackResponse>> CreateStackAsync(CreateStackRequest request, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);
                var payload = new
                {
                    name = request.Name,
                    description = request.Description
                };

                var response = await client.PostAsJsonAsync("api/stacks/create", payload, ct);
                return await parser.ParseAsync<StackResponse>(response, ct);
            }
            catch (Exception ex)
            {
                return ApiResult<StackResponse>.Fail(ex.Message);
            }
        }

        public async Task<ApiResult<StackResponse>> UpdateStackAsync(int id, UpdateStackRequest request, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);
                var payload = new
                {
                    name = request.Name,
                    description = request.Description
                };

                var response = await client.PutAsJsonAsync($"api/stacks/update/{id}", payload, ct);
                return await parser.ParseAsync<StackResponse>(response, ct);
            }
            catch (Exception ex)
            {
                return ApiResult<StackResponse>.Fail(ex.Message);
            }
        }

        public async Task<ApiResult> DeleteStackAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.DeleteAsync($"api/stacks/delete/{id}", ct);
                return await parser.ParseAsync(response, ct);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }
    }
}
