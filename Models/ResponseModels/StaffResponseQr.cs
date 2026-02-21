namespace ZoomAttendance.Models.ResponseModels
{
    public class StaffResponseQr
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string BarcodeToken { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
