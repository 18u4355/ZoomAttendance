namespace ZoomAttendance.Models.ResponseModels
{
    public class MeetingAttendanceResponse
    {
        public int AttendanceId { get; set; }
        public string StaffEmail { get; set; }
        public string StaffName { get; set; }
        public DateTime? JoinTime { get; set; }
        public bool ConfirmAttendance { get; set; }
        public DateTime? ConfirmationTime { get; set; }
         
    }
}
