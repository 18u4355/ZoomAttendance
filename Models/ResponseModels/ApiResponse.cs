
namespace ZoomAttendance.Models.ResponseModels
{
    public class ApiResponse<T>
    {
        public bool IsSuccessful { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? Details { get; set; }

        public static ApiResponse<T> Success(T data, string? message = null)
            => new() {  Data = data, IsSuccessful = true, Message = message };

        public static ApiResponse<T> Fail(string message, string? details = null)
            => new() { IsSuccessful = false, Data = default, Message = message, Details = details };

        internal static ApiResponse<List<AttendanceReportResponse>> Success(List<AttendanceReportResponse> report, string v)
        {
            throw new NotImplementedException();
        }
    }
}
