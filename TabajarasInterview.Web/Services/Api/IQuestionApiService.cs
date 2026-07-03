using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// Contract for the questions API client. All calls target the rust-api
    /// over <c>HttpClient</c>; the Blazor app never touches the database.
    /// </summary>
    public interface IQuestionApiService
    {
        /// <summary>Lists questions (<c>GET /api/questions/get_all</c>).</summary>
        Task<ApiResult<List<QuestionResponse>>> GetQuestionsAsync(CancellationToken ct = default);

        /// <summary>Fetches a single question (<c>GET /api/questions/get/{id}</c>).</summary>
        Task<ApiResult<QuestionResponse>> GetQuestionByIdAsync(int id, CancellationToken ct = default);

        /// <summary>Creates a question (<c>POST /api/questions/create</c>).</summary>
        Task<ApiResult<QuestionResponse>> CreateQuestionAsync(CreateQuestionRequest request, CancellationToken ct = default);

        /// <summary>Updates a question (<c>PUT /api/questions/update/{id}</c>).</summary>
        Task<ApiResult<QuestionResponse>> UpdateQuestionAsync(int id, UpdateQuestionRequest request, CancellationToken ct = default);

        /// <summary>Soft-deletes a question (<c>DELETE /api/questions/delete/{id}</c>).</summary>
        Task<ApiResult> DeleteQuestionAsync(int id, CancellationToken ct = default);
    }
}
