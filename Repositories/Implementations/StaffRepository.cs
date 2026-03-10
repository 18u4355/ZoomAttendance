// Repositories/Implementations/StaffRepository.cs
// Full redraft — Id is now Guid everywhere

using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using ZoomAttendance.Helpers;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Repositories.Implementations
{
    public class StaffRepository : IStaffRepository
    {
        private readonly string _connectionString;

        public StaffRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // ── Get All ───────────────────────────────────────────────────────────
        public async Task<PagedStaffResponse> GetAllAsync(StaffFilterRequest filter)
        {
            var records = new List<StaffResponse>();
            int total = 0;

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAllStaff", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Search", (object?)filter.Search ?? DBNull.Value);
            command.Parameters.AddWithValue("@DepartmentId", (object?)filter.DepartmentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (object?)filter.Status ?? DBNull.Value);
            command.Parameters.AddWithValue("@Page", filter.Page);
            command.Parameters.AddWithValue("@Limit", filter.Limit);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (total == 0) total = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                records.Add(MapFromReader(reader));
            }

            return new PagedStaffResponse
            {
                Data = records,
                Total = total,
                Page = filter.Page,
                Limit = filter.Limit,
                TotalPages = (int)Math.Ceiling((double)total / filter.Limit)
            };
        }

        // ── Get By Id ─────────────────────────────────────────────────────────
        public async Task<StaffResponse?> GetByIdAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetStaffById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            return await reader.ReadAsync() ? MapFromReader(reader) : null;
        }

        // ── Create ────────────────────────────────────────────────────────────
        public async Task<StaffResponse> CreateAsync(CreateStaffRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CreateStaff", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Name", request.Name);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@DepartmentId", request.DepartmentId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode))
                    throw new InvalidOperationException($"{errorCode}:{reader["ErrorMessage"]}");

                var insertedId = reader.GetGuid(reader.GetOrdinal("InsertedId"));
                await reader.CloseAsync();
                return (await GetByIdAsync(insertedId))!;
            }

            throw new InvalidOperationException("Unexpected error during staff creation.");
        }

        // ── Update ────────────────────────────────────────────────────────────
        public async Task<StaffResponse> UpdateAsync(Guid id, UpdateStaffRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_UpdateStaff", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", request.Name);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@DepartmentId", request.DepartmentId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode))
                    throw new InvalidOperationException($"{errorCode}:{reader["ErrorMessage"]}");

                return MapFromReader(reader);
            }

            throw new InvalidOperationException("Unexpected error during staff update.");
        }

        // ── Delete ────────────────────────────────────────────────────────────
        public async Task DeleteAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_DeleteStaff", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode))
                    throw new InvalidOperationException($"{errorCode}:{reader["ErrorMessage"]}");
            }
        }

        // ── Update Status ─────────────────────────────────────────────────────
        public async Task UpdateStatusAsync(Guid id, string status)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_UpdateStaffStatus", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Status", status);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode))
                    throw new InvalidOperationException($"{errorCode}:{reader["ErrorMessage"]}");
            }
        }

        // ── Bulk Upload ───────────────────────────────────────────────────────
        public async Task<BulkUploadResponse> BulkUploadAsync(IFormFile file)
        {
            var response = new BulkUploadResponse();
            var rows = new List<BulkUploadStaffRow>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                for (int r = 5; r <= lastRow; r++)
                {
                    var nameVal = worksheet.Cell(r, 1).GetString().Trim();
                    var emailVal = worksheet.Cell(r, 2).GetString().Trim();
                    var deptVal = worksheet.Cell(r, 3).GetString().Trim();

                    if (string.IsNullOrEmpty(nameVal) && string.IsNullOrEmpty(emailVal))
                        continue;

                    int.TryParse(deptVal, out int deptId);
                    rows.Add(new BulkUploadStaffRow { RowNumber = r, Name = nameVal, Email = emailVal, DepartmentId = deptId });
                }
            }

            if (rows.Count == 0)
                throw new InvalidOperationException("No data rows found in the uploaded file.");
            if (rows.Count > 500)
                throw new InvalidOperationException("Maximum 500 rows allowed per upload.");

            response.TotalRows = rows.Count;
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var result = new BulkUploadRowResult { RowNumber = row.RowNumber, Name = row.Name, Email = row.Email };

                if (string.IsNullOrEmpty(row.Name))
                { result.Success = false; result.Error = "Name is required."; response.Results.Add(result); response.Failed++; continue; }
                if (string.IsNullOrEmpty(row.Email) || !row.Email.Contains('@'))
                { result.Success = false; result.Error = "Valid email is required."; response.Results.Add(result); response.Failed++; continue; }
                if (row.DepartmentId <= 0)
                { result.Success = false; result.Error = "Valid DepartmentId is required."; response.Results.Add(result); response.Failed++; continue; }
                if (seenEmails.Contains(row.Email))
                { result.Success = false; result.Error = "Duplicate email within the uploaded file."; response.Results.Add(result); response.Failed++; continue; }

                seenEmails.Add(row.Email);

                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    using var command = new SqlCommand("sp_BulkCreateStaff", connection) { CommandType = CommandType.StoredProcedure };
                    command.Parameters.AddWithValue("@Name", row.Name);
                    command.Parameters.AddWithValue("@Email", row.Email);
                    command.Parameters.AddWithValue("@DepartmentId", row.DepartmentId);

                    await connection.OpenAsync();
                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        var errorCode = reader["ErrorCode"]?.ToString();
                        if (!string.IsNullOrEmpty(errorCode))
                        { result.Success = false; result.Error = reader["ErrorMessage"].ToString(); response.Failed++; }
                        else
                        {
                            result.Success = true;
                            result.StaffId = reader.IsDBNull(reader.GetOrdinal("InsertedId")) ? null : reader.GetGuid(reader.GetOrdinal("InsertedId"));
                            response.Succeeded++;
                        }
                    }
                }
                catch (Exception ex)
                { result.Success = false; result.Error = $"Unexpected error: {ex.Message}"; response.Failed++; }

                response.Results.Add(result);
            }

            return response;
        }

        // ── Export ────────────────────────────────────────────────────────────
        public async Task<byte[]> ExportAsync(StaffFilterRequest filter)
        {
            var records = new List<StaffResponse>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAllStaff", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Search", (object?)filter.Search ?? DBNull.Value);
            command.Parameters.AddWithValue("@DepartmentId", (object?)filter.DepartmentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (object?)filter.Status ?? DBNull.Value);
            command.Parameters.AddWithValue("@Page", 1);
            command.Parameters.AddWithValue("@Limit", 10000);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                records.Add(MapFromReader(reader));

            var headers = new[] { "Name", "Email", "Department", "Status", "Created At" };
            var rows = records.Select(s => new List<object?>
            {
                s.Name, s.Email, s.DepartmentName, s.Status,
                s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return ExcelExportHelper.GenerateExcel("Staff", headers, rows);
        }

        // ── Download Template ─────────────────────────────────────────────────
        public async Task<byte[]> GetUploadTemplateAsync()
        {
            var departments = new List<string>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT TOP 10 Id, Name FROM dbo.Departments ORDER BY Id", connection)
                { CommandType = CommandType.Text };
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    departments.Add($"{reader.GetInt32(0)} = {reader.GetString(1)}");
            }
            catch { }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Staff Upload");

            ws.Range("A1:C1").Merge();
            ws.Cell("A1").Value = "MeetTrack – Staff Bulk Upload Template";
            ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(13).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1A5FAB"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(1).Height = 28;

            ws.Range("A2:C2").Merge();
            var hint = departments.Count > 0
                ? $"DepartmentId values: {string.Join(", ", departments)}"
                : "DepartmentId must match an existing department (GET /api/v1/departments).";
            ws.Cell("A2").Value = $"Fill in staff details below. Do not modify column headers. {hint} All staff default to Active.";
            ws.Cell("A2").Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#7F7F7F")).Font.SetItalic(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#FFF3CD")).Alignment.SetWrapText(true);
            ws.Row(2).Height = 22;

            string[] hdrs = { "Name *", "Email *", "DepartmentId *" };
            int[] widths = { 32, 38, 20 };
            for (int i = 0; i < hdrs.Length; i++)
            {
                ws.Cell(3, i + 1).Value = hdrs[i];
                ws.Cell(3, i + 1).Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2D8CFF"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Column(i + 1).Width = widths[i];
            }
            ws.Row(3).Height = 22;

            string[] subNotes = { "Full name of staff member", "Work email – must be unique", "Numeric ID e.g. 1, 2, 3" };
            for (int i = 0; i < subNotes.Length; i++)
            {
                ws.Cell(4, i + 1).Value = subNotes[i];
                ws.Cell(4, i + 1).Style.Font.SetFontSize(8).Font.SetFontColor(XLColor.FromHtml("#888888")).Font.SetItalic(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
            ws.Row(4).Height = 16;

            object[][] examples = {
                new object[] { "John Doe",   "john.doe@company.com",   1 },
                new object[] { "Jane Smith", "jane.smith@company.com", 1 },
            };
            for (int r = 0; r < examples.Length; r++)
                for (int c = 0; c < examples[r].Length; c++)
                {
                    ws.Cell(5 + r, c + 1).Value = examples[r][c].ToString();
                    ws.Cell(5 + r, c + 1).Style.Font.SetFontColor(XLColor.FromHtml("#555555")).Font.SetItalic(true)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#EBF3FF"));
                }

            for (int r = 7; r <= 37; r++) ws.Row(r).Height = 18;
            ws.SheetView.FreezeRows(4);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return await Task.FromResult(stream.ToArray());
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private static StaffResponse MapFromReader(SqlDataReader reader)
        {
            return new StaffResponse
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
                DepartmentName = reader.GetString(reader.GetOrdinal("DepartmentName")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            };
        }
    }
}