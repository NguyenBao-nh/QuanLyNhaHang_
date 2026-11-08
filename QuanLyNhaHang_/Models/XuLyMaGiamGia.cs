using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;

namespace QuanLyNhaHang_.Services
{
    /// <summary>
    /// Thay cho XuLyGiamGia cũ (LayGiaTriGiamGia / KiemTraMaGiamGia / ThemMaGiamGia có SoTien).
    /// Làm việc trên bảng MaGiamGia (model MaGiamGia có SoTien) vì entity GiamGia mới không có cột SoTien.
    /// </summary>
    public class XuLyMaGiamGia
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyMaGiamGia(DbQlNhaHangContext context)
        {
            _context = context;
        }

        public async Task<MaGiamGia?> LayMaHopLeAsync(string? ma)
        {
            if (string.IsNullOrWhiteSpace(ma)) return null;
            ma = ma.Trim();
            var now = DateTime.Now;
            var homNay = DateTime.Today;   // NgayKT lưu lúc 00:00 nên phải so với đầu ngày, nếu không mã hết hạn sớm 1 ngày
            return await _context.MaGiamGia.AsNoTracking()
                .FirstOrDefaultAsync(g => g.MaGiamGia_Code == ma
                                       && g.SoLuong > 0
                                       && g.NgayBD <= now
                                       && g.NgayKT >= homNay);
        }

        public async Task<bool> KiemTraAsync(string? ma) => await LayMaHopLeAsync(ma) != null;

        /// <summary>Số tiền được giảm cho hóa đơn có tổng <paramref name="tong"/> (0 nếu mã không hợp lệ).
        /// Mã theo phần trăm: tong * PhanTram%, không vượt mức tối đa (SoTien) nếu có.</summary>
        public async Task<int> LayGiaTriGiamGiaAsync(string? ma, int tong)
            => (await LayMaHopLeAsync(ma))?.TinhSoTienGiam(tong) ?? 0;

        /// <summary>Trả lại 1 lượt dùng cho mã (đơn trực tuyến bị hủy).</summary>
        public async Task TraLaiSoLuongAsync(string? ma)
        {
            if (string.IsNullOrWhiteSpace(ma)) return;
            var g = await _context.MaGiamGia.FindAsync(ma.Trim());
            if (g != null)
            {
                g.SoLuong += 1;
                await _context.SaveChangesAsync();
            }
        }

        public async Task GiamSoLuongAsync(string? ma)
        {
            if (string.IsNullOrWhiteSpace(ma)) return;
            var g = await _context.MaGiamGia.FindAsync(ma.Trim());
            if (g != null && g.SoLuong > 0)
            {
                g.SoLuong -= 1;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ThemAsync(string ma, DateTime ngayBD, DateTime ngayKT, int soLuong, int soTien)
        {
            _context.MaGiamGia.Add(new MaGiamGia
            {
                MaGiamGia_Code = ma,
                NgayBD = ngayBD.Date,
                NgayKT = ngayKT.Date,
                SoLuong = soLuong,
                SoTien = soTien
            });
            await _context.SaveChangesAsync();
        }
    }
}
