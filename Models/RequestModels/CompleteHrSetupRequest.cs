namespace ZoomAttendance.Models.RequestModels
{
    public class CompleteHrSetupRequest
    {
        public string Token { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}