
namespace ZoomAttendance.Models.ResponseModels
{
    public class SendQrCodeResponse
    {
        public int TotalSelected { get; set; }
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public List<QrCodeEmailResult> Results { get; set; } = new(); // fixed — removed Responses. prefix
    }

}