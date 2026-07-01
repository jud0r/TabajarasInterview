using TabajarasInterview.Web.DTOs.Dashboard;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    public interface IDashboardApiService
    {
        /// <summary>
        /// Fetches the aggregated recruitment dashboard overview
        /// (<c>GET /api/dashboard/overview</c>), optionally filtered by date range,
        /// position and interview type.
        /// </summary>
        Task<ApiResult<DashboardData>> GetOverviewAsync(DashboardFilter filter, CancellationToken ct = default);
    }
}
