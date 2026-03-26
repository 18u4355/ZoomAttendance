// Repositories/Implementations/DepartmentRepository.cs

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using ZoomAttendance.Helpers;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Repositories.Implementations
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly string _connectionString;

        public DepartmentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // ── Get All ───────────────────────────────────────────────────────────
        public async Task<IEnumerable<DepartmentResponse>> GetAllAsync(
      string? status = null,
      int pageNumber = 1,
      int pageSize = 10)
        {
            var departments = new List<DepartmentResponse>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAllDepartments", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
            command.Parameters.AddWithValue("@PageNumber", pageNumber);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                departments.Add(MapToResponse(reader));

            return departments;
        }
        // ── Get By Id ─────────────────────────────────────────────────────────
        public async Task<DepartmentResponse?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetDepartmentById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            return await reader.ReadAsync() ? MapToResponse(reader) : null;
        }

        // ── Create ────────────────────────────────────────────────────────────
        public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CreateDepartment", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Name", request.Name.Trim());

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                if (reader.GetName(0) == "ErrorCode")
                    throw new InvalidOperationException(reader["ErrorMessage"].ToString());

                return MapToResponse(reader);
            }

            throw new InvalidOperationException("Failed to create department.");
        }

        // ── Update ────────────────────────────────────────────────────────────
        public async Task<DepartmentResponse> UpdateAsync(int id, UpdateDepartmentRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_UpdateDepartment", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", request.Name.Trim());

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                if (reader.GetName(0) == "ErrorCode")
                {
                    var errorCode = reader["ErrorCode"].ToString();
                    var errorMessage = reader["ErrorMessage"].ToString();
                    if (errorCode == "NOT_FOUND") throw new KeyNotFoundException(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                return MapToResponse(reader);
            }

            throw new InvalidOperationException("Failed to update department.");
        }

        // ── Deactivate ───────────────────────────────────────────────────────
        public async Task DeactivateAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_DeactivateDepartment", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                var errorMessage = reader["ErrorMessage"]?.ToString();

                if (!string.IsNullOrEmpty(errorCode))
                {
                    if (errorCode == "NOT_FOUND") throw new KeyNotFoundException(errorMessage);
                    if (errorCode == "ALREADY_INACTIVE") throw new InvalidOperationException(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
            }
        }

        // ── Activate ───────────────────────────────────────────────────────────
        public async Task<DepartmentResponse> ActivateAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_ActivateDepartment", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                if (reader.GetName(0) == "ErrorCode")
                {
                    var errorCode = reader["ErrorCode"].ToString();
                    var errorMessage = reader["ErrorMessage"].ToString();

                    if (errorCode == "NOT_FOUND") throw new KeyNotFoundException(errorMessage);
                    if (errorCode == "ALREADY_ACTIVE") throw new InvalidOperationException(errorMessage);

                    throw new InvalidOperationException(errorMessage);
                }

                return MapToResponse(reader);
            }

            throw new InvalidOperationException("Failed to activate department.");
        }

        // ── Export ────────────────────────────────────────────────────────────
        public async Task<byte[]> ExportAsync(bool includeInactive = false)
        {
            var records = new List<DepartmentResponse>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_ExportDepartments", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                records.Add(MapToResponse(reader));

            var headers = new[] { "Id", "Name", "Active", "Staff Count", "Meeting Count", "Created At", "Updated At" };
            var rows = records.Select(d => new List<object?>
            {
                d.Id, d.Name,
                d.IsActive ? "Yes" : "No",
                d.StaffCount,
                d.MeetingCount,
                d.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                d.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return ExcelExportHelper.GenerateExcel("Departments", headers, rows);
        }

        // ── Mapper ────────────────────────────────────────────────────────────
        private static DepartmentResponse MapToResponse(SqlDataReader reader) => new()
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            StaffCount = reader.GetInt32(reader.GetOrdinal("StaffCount")),
            MeetingCount = reader.GetInt32(reader.GetOrdinal("MeetingCount")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }
}