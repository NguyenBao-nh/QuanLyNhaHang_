using Microsoft.EntityFrameworkCore;

namespace QuanLyNhaHang_.Services
{
    public static class SinhMaHelper
    {
        /// <summary>
        /// Sinh mã tiếp theo dạng PREFIX0001 (lấy số lớn nhất hiện có + 1, không so sánh chuỗi).
        /// </summary>
        public static async Task<string> TaoMaTiepTheoAsync(IQueryable<string> cacMaQuery, string prefix, int width = 4)
        {
            var cacMa = await cacMaQuery.AsNoTracking().ToListAsync();
            int max = 0;
            foreach (var ma in cacMa)
            {
                var m = ma?.Trim();
                if (!string.IsNullOrEmpty(m) &&
                    m.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(m.AsSpan(prefix.Length), out int so) &&
                    so > max)
                {
                    max = so;
                }
            }
            return prefix + (max + 1).ToString("D" + width);
        }
    }
}
