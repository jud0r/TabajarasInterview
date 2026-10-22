using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// <see cref="IQuestionApiService"/> implementation backed by the API.
    /// Every request is authorized (the question endpoints require a valid JWT) and
    /// parsed through <see cref="ApiResponseParserService"/> into an <see cref="ApiResult"/>.
    /// </summary>
    public class QuestionApiService(
        AuthorizedHttpClientFactory authorizedFactory,
        ApiResponseParserService parser) : IQuestionApiService
    {
        private const string ClientName = "api";

        public async Task<ApiResult<List<QuestionResponse>>> GetQuestionsAsync(CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.GetAsync("api/questions/getAll", ct);
                return await parser.ParseAsync<List<QuestionResponse>>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<List<QuestionResponse>>.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult<QuestionResponse>> GetQuestionByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.GetAsync($"api/questions/get/{id}", ct);
                return await parser.ParseAsync<QuestionResponse>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<QuestionResponse>.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult<QuestionResponse>> CreateQuestionAsync(CreateQuestionRequest request, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);
                var payload = new
                {
                    stackId = request.StackId,
                    question = request.Question,
                    acceptableAnswer = request.AcceptableAnswer,
                    level = request.Level
                };

                var response = await client.PostAsJsonAsync("api/questions/create", payload, ct);
                return await parser.ParseAsync<QuestionResponse>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<QuestionResponse>.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult<QuestionResponse>> UpdateQuestionAsync(int id, UpdateQuestionRequest request, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);
                var payload = new
                {
                    stackId = request.StackId,
                    question = request.Question,
                    acceptableAnswer = request.AcceptableAnswer,
                    level = request.Level
                };

                var response = await client.PutAsJsonAsync($"api/questions/update/{id}", payload, ct);
                return await parser.ParseAsync<QuestionResponse>(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<QuestionResponse>.Fail(parser.Describe(ex));
            }
        }

        public async Task<ApiResult> DeleteQuestionAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var client = await authorizedFactory.CreateClientAsync(ClientName);

                var response = await client.DeleteAsync($"api/questions/delete/{id}", ct);
                return await parser.ParseAsync(response, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult.Fail(parser.Describe(ex));
            }
        }
    }
}
