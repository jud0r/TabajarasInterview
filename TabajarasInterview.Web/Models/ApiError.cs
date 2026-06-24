namespace TabajarasInterview.Web.Models
{
    public class ApiError
    {
        public string? Code { get; set; }
        public string? Error { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
