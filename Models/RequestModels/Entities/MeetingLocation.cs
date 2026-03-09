// Entities/MeetingLocation.cs

namespace ZoomAttendance.Entities
{
    public class MeetingLocation
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int RadiusMetres { get; set; } = 100;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}