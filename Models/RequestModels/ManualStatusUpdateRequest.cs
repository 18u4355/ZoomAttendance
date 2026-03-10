public class ManualStatusUpdateRequest
{
    public int MeetingId { get; set; }
    public Guid StaffId { get; set; }
    public string Status { get; set; } = string.Empty;
}