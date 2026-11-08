using Microsoft.EntityFrameworkCore;

namespace QuanLyNhaHang_.Models
{
    // Dữ liệu cho trang in hóa đơn (Views/Shared/InHoaDon.cshtml)
    public class HoaDonInViewModel
    {
        public HoaDon HoaDon { get; set; } = null!;
        public List<ChiTietHoaDon> ChiTiet { get; set; } = new();
        public KhachHang? Khach { get; set; }
        public NhanVien? NhanVien { get; set; }

        public int TongTienMon => ChiTiet.Sum(c => c.Thanhtien ?? 0);
        public int DaThanhToan => HoaDon.Tongtien ?? 0;
        public int GiamGia => Math.Max(0, TongTienMon - DaThanhToan);

        public static async Task<HoaDonInViewModel?> TaiAsync(DbQlNhaHangContext db, string ma)
        {
            ma = ma.Trim();
            var hd = await db.HoaDons.AsNoTracking()
                .Include(h => h.MakhNavigation)
                .Include(h => h.ManvNavigation)
                .FirstOrDefaultAsync(h => h.Mahoadon == ma);
            if (hd == null) return null;

            var ct = await db.ChiTietHoaDons.AsNoTracking()
                .Include(c => c.MamonanNavigation)
                .Where(c => c.Mahoadon == ma)
                .ToListAsync();

            return new HoaDonInViewModel
            {
                HoaDon = hd,
                ChiTiet = ct,
                Khach = hd.MakhNavigation,
                NhanVien = hd.ManvNavigation
            };
        }
    }
}
