namespace ZoomAttendance.Models.RequestModels
{
    public class CreateVenueRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int RadiusMetres { get; set; } = 20;
    }

    public class UpdateVenueRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int RadiusMetres { get; set; }
    }
}
