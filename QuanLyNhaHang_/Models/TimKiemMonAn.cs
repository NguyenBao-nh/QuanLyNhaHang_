using System.Globalization;
using System.Text;

namespace QuanLyNhaHang_.Models
{
    /// <summary>
    /// Tìm món ăn theo MÃ hoặc TÊN, không phân biệt hoa/thường và có dấu/không dấu
    /// (gõ "ga", "gà", "MA00" đều ra kết quả). Dùng chung cho trang admin và trang Menu.
    /// </summary>
    public static class TimKiemMonAn
    {
        // Danh sách loại món dùng cho ô chọn (select) khi thêm / sửa món
        public static readonly string[] LoaiMonMacDinh =
        {
            "Khai vị", "Món chính", "Món xào", "Canh - Súp", "Cơm - Bún",
            "Ăn nhẹ", "Lẩu", "Tráng miệng", "Đồ uống"
        };

        public static string BoDau(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var chuan = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(chuan.Length);
            foreach (var c in chuan)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd');
        }

        /// <summary>Điểm khớp: 0 = không khớp; càng lớn càng khớp (để sắp xếp gợi ý).</summary>
        public static int DiemKhop(MonAn m, string tuKhoa)
        {
            var q = BoDau(tuKhoa);
            if (q == "") return 1;

            var ma = BoDau(m.Mamonan);
            var ten = BoDau(m.Tenmon);

            if (ma == q) return 100;
            if (ma.StartsWith(q)) return 80;
            if (ten.StartsWith(q)) return 70;
            if (ten.Split(' ').Any(tu => tu.StartsWith(q))) return 60;
            if (ten.Contains(q)) return 50;
            if (ma.Contains(q)) return 40;
            // Gõ nhiều từ: mọi từ đều xuất hiện trong tên
            var cacTu = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (cacTu.Length > 1 && cacTu.All(t => ten.Contains(t))) return 30;
            return 0;
        }

        public static List<MonAn> Loc(IEnumerable<MonAn> ds, string? tuKhoa)
        {
            if (string.IsNullOrWhiteSpace(tuKhoa)) return ds.ToList();
            return ds.Select(m => (m, d: DiemKhop(m, tuKhoa)))
                     .Where(x => x.d > 0)
                     .OrderByDescending(x => x.d)
                     .ThenBy(x => x.m.Mamonan)
                     .Select(x => x.m)
                     .ToList();
        }
    }
}
