namespace ZoomAttendance.Auth
{
    public class OtpService
    {
        public string GenerateOtp()
        {
            try
            {
                // Implement OTP logic here
                return new Random().Next(100000, 999999).ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating OTP", ex);
            }
        }
    }
}
