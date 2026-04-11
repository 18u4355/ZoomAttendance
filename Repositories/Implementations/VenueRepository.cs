using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Repositories.Implementations
{
    public class VenueRepository : IVenueRepository
    {
        private readonly string _connectionString;

        public VenueRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<IEnumerable<VenueResponse>> GetAllAsync(bool includeInactive = false)
        {
            var results = new List<VenueResponse>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAllVenues", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(MapRow(reader));
            return results;
        }

        public async Task<VenueResponse?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetVenueById", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Id", id);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return MapRow(reader);
        }

        public async Task<VenueResponse> CreateAsync(CreateVenueRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CreateVenue", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Name", request.Name.Trim());
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Latitude", request.Latitude);
            command.Parameters.AddWithValue("@Longitude", request.Longitude);
            command.Parameters.AddWithValue("@RadiusMetres", request.RadiusMetres);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("Failed to create venue.");

        
            if (reader.FieldCount == 2)
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode))
                    throw new InvalidOperationException($"{errorCode}:{reader["ErrorMessage"]}");
            }

            return MapRow(reader);
        }

        public async Task<VenueResponse> UpdateAsync(int id, UpdateVenueRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_UpdateVenue", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", request.Name.Trim());
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Latitude", request.Latitude);
            command.Parameters.AddWithValue("@Longitude", request.Longitude);
            command.Parameters.AddWithValue("@RadiusMetres", request.RadiusMetres);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("Failed to update venue.");

            if (reader.FieldCount == 2)
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode))
                {
                    if (errorCode == "NOT_FOUND")
                        throw new KeyNotFoundException(reader["ErrorMessage"].ToString());
                    throw new InvalidOperationException($"{errorCode}:{reader["ErrorMessage"]}");
                }
            }

            return MapRow(reader);
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_DeleteVenue", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Id", id);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode))
                {
                    if (errorCode == "NOT_FOUND")
                        throw new KeyNotFoundException(reader["ErrorMessage"].ToString());
                    throw new InvalidOperationException(reader["ErrorMessage"].ToString());
                }
            }
        }

        private static VenueResponse MapRow(SqlDataReader reader) => new()
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            Latitude = reader.GetDecimal(reader.GetOrdinal("Latitude")),
            Longitude = reader.GetDecimal(reader.GetOrdinal("Longitude")),
            RadiusMetres = reader.GetInt32(reader.GetOrdinal("RadiusMetres")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            MeetingCount = reader.GetInt32(reader.GetOrdinal("MeetingCount")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
        };
    }
}