// Helpers/ExcelExportHelper.cs

using ClosedXML.Excel;

namespace ZoomAttendance.Helpers
{
    public static class ExcelExportHelper
    {
        public static byte[] GenerateExcel(
            string sheetName,
            string[] headers,
            IEnumerable<IEnumerable<object?>> rows)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            // ── Header row ────────────────────────────────────────────────────
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2d8cff");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // ── Data rows ─────────────────────────────────────────────────────
            int rowIndex = 2;
            foreach (var row in rows)
            {
                int colIndex = 1;
                foreach (var value in row)
                {
                    worksheet.Cell(rowIndex, colIndex).Value = value?.ToString() ?? string.Empty;
                    colIndex++;
                }
                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}