namespace ZoomAttendance.Helpers
{
    public class UserContextHelper
    {
        public int GetCurrentUserId()
        {
            try
            {
                // Example: get user ID from JWT claims
                return 0; // placeholder
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching current user ID", ex);
            }
        }
    }
}
