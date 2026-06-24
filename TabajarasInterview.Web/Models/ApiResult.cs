namespace TabajarasInterview.Web.Models
{
    public class ApiResult
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, string[]>? ValidationErrors { get; set; }

        public static ApiResult Ok() => new() { Success = true };

        public static ApiResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };

        public static ApiResult Validation(Dictionary<string, string[]> errors) =>
            new() { Success = false, ValidationErrors = errors };
    }

    public class ApiResult<T> : ApiResult
    {
        public T? Data { get; set; }

        public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };

        public static new ApiResult<T> Fail(string message) =>
            new() { Success = false, ErrorMessage = message };

        public static new ApiResult<T> Validation(Dictionary<string, string[]> errors) =>
            new() { Success = false, ValidationErrors = errors };
    }
}
