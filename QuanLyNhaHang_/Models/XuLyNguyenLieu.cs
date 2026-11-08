using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;

namespace QuanLyNhaHang_.Services
{
    public class XuLyNguyenLieu
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyNguyenLieu(DbQlNhaHangContext context)
        {
            _context = context;
        }

        // Lấy danh sách nguyên liệu kèm số lượng tồn (tính = tổng nhập - tổng xuất).
        // Bảng NguyenLieu trong DB không có cột SOLUONGTON nên Soluongton là thuộc tính [NotMapped],
        // được gán ở đây thay vì đọc từ DB.
        public async Task<List<NguyenLieu>> LayThongTinNguyenLieuAsync()
        {
            var ds = await _context.NguyenLieus
                .AsNoTracking()
                .OrderBy(n => n.Tennguyenlieu)
                .ToListAsync();

            var ton = await TinhTonKhoAsync();
            foreach (var nl in ds)
            {
                nl.Soluongton = ton.TryGetValue(nl.Manguyenlieu.Trim(), out var t) ? t : 0;
            }
            return ds;
        }

        /// <summary>Mức tồn tối thiểu: "CanhBaoTonKho": { "MucToiThieu": 5 } trong appsettings.json (mặc định 5).</summary>
        public static int MucToiThieu(IConfiguration cfg) => Math.Max(0, cfg.GetValue<int?>("CanhBaoTonKho:MucToiThieu") ?? 5);

        /// <summary>Nguyên liệu có tồn &lt;= mức tối thiểu (hết hàng xếp trước, rồi tồn ít nhất).</summary>
        public async Task<List<NguyenLieu>> LayNguyenLieuSapHetAsync(int mucToiThieu)
        {
            var ds = await LayThongTinNguyenLieuAsync();
            return ds.Where(n => (n.Soluongton ?? 0) <= mucToiThieu)
                     .OrderBy(n => n.Soluongton ?? 0).ThenBy(n => n.Tennguyenlieu)
                     .ToList();
        }

        // Trả về tồn kho theo mã nguyên liệu (key đã Trim). Truyền null để tính cho tất cả nguyên liệu.
        public async Task<Dictionary<string, int>> TinhTonKhoAsync(IEnumerable<string>? dsMa = null)
        {
            var ma = dsMa?.Select(m => m.Trim()).ToList();

            var qNhap = _context.ChiTietNhapKhos.AsNoTracking().AsQueryable();
            var qXuat = _context.ChiTietXuatKhos.AsNoTracking().AsQueryable();
            if (ma != null)
            {
                qNhap = qNhap.Where(c => ma.Contains(c.Manguyenlieu));
                qXuat = qXuat.Where(c => ma.Contains(c.Manguyenlieu));
            }

            var nhap = await qNhap
                .GroupBy(c => c.Manguyenlieu)
                .Select(g => new { Ma = g.Key, Tong = g.Sum(x => (long)(x.Soluong ?? 0)) })
                .ToListAsync();

            var xuat = await qXuat
                .GroupBy(c => c.Manguyenlieu)
                .Select(g => new { Ma = g.Key, Tong = g.Sum(x => (long)(x.Soluong ?? 0)) })
                .ToListAsync();

            var kq = new Dictionary<string, long>();
            foreach (var n in nhap) kq[n.Ma.Trim()] = kq.GetValueOrDefault(n.Ma.Trim()) + n.Tong;
            foreach (var x in xuat) kq[x.Ma.Trim()] = kq.GetValueOrDefault(x.Ma.Trim()) - x.Tong;

            return kq.ToDictionary(
                p => p.Key,
                p => (int)Math.Clamp(p.Value, int.MinValue, int.MaxValue));
        }
    }
}
