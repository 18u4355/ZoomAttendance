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

        public async Task<IEnumerable<DepartmentResponse>> GetAllAsync()
        {
            var departments = new List<DepartmentResponse>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAllDepartments", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                departments.Add(MapToResponse(reader));
            }

            return departments;
        }

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

            if (await reader.ReadAsync())
                return MapToResponse(reader);

            return null;
        }

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
                // Check if SP returned an error
                if (reader.GetName(0) == "ErrorCode")
                    throw new InvalidOperationException(reader["ErrorMessage"].ToString());

                return MapToResponse(reader);
            }

            throw new InvalidOperationException("Failed to create department.");
        }

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

                    if (errorCode == "NOT_FOUND")
                        throw new KeyNotFoundException(errorMessage);

                    throw new InvalidOperationException(errorMessage);
                }

                return MapToResponse(reader);
            }

            throw new InvalidOperationException("Failed to update department.");
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_DeleteDepartment", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"].ToString();
                var errorMessage = reader["ErrorMessage"].ToString();

                if (errorCode == "NOT_FOUND")
                    throw new KeyNotFoundException(errorMessage);

                if (errorCode == "HAS_ACTIVE_STAFF")
                    throw new InvalidOperationException(errorMessage);
            }
        }
        // Add this method to DepartmentRepository.cs
        // inside the class body

        public async Task<byte[]> ExportAsync()
        {
            var records = new List<DepartmentResponse>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_ExportDepartments", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                records.Add(new DepartmentResponse
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    StaffCount = reader.GetInt32(reader.GetOrdinal("StaffCount")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                });
            }

            var headers = new[] { "Id", "Name", "Staff Count", "Created At", "Updated At" };

            var rows = records.Select(d => new List<object?>
            {
                d.Id,
                d.Name,
                d.StaffCount,
                d.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                d.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return ExcelExportHelper.GenerateExcel("Departments", headers, rows);
        }

        // ── Mapper ───────────────────────────────────────────────────
        private static DepartmentResponse MapToResponse(SqlDataReader reader) => new()
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            StaffCount = reader.GetInt32(reader.GetOrdinal("StaffCount")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }
}