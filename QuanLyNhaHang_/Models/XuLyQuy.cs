using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;

namespace QuanLyNhaHang_.Services
{
    /// <summary>
    /// Tính doanh thu / chi tiêu / xuất kho theo khoảng thời gian.
    /// Dùng chung cho trang Quỹ (nhân viên) và trang Doanh thu (quản lý).
    /// </summary>
    public class XuLyQuy
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyQuy(DbQlNhaHangContext context)
        {
            _context = context;
        }

        // Doanh thu: hóa đơn tại quán (chưa hủy) + đơn trực tuyến nhà hàng đã xác nhận nhận tiền
        public IQueryable<HoaDon> HoaDonTinhDoanhThu()
            => _context.HoaDons.AsNoTracking()
                .Where(h => (h.Loaidon != LoaiDon.TrucTuyen && h.Trangthai != TrangThaiDon.DaHuy)
                         || (h.Loaidon == LoaiDon.TrucTuyen && (h.Trangthai == TrangThaiDon.DaXacNhan || h.Trangthai == TrangThaiDon.DaGiao)));

        // Hóa đơn tính doanh thu trong 1 ngày
        public Task<List<HoaDonTrongNgay>> HoaDonTrongNgayAsync(DateTime ngay)
        {
            var tu = ngay.Date;
            var den = tu.AddDays(1);
            return HoaDonTinhDoanhThu()
                .Where(h => h.Ngaylap >= tu && h.Ngaylap < den)
                .OrderByDescending(h => h.Ngaylap)
                .Select(h => new HoaDonTrongNgay
                {
                    MaHoaDon = h.Mahoadon,
                    NgayLap = h.Ngaylap,
                    LoaiDon = h.Loaidon,
                    HinhThuc = h.Hinhthuc,
                    TenKhach = h.Tenkhach,
                    MaBan = h.Maban,
                    TongTien = h.Tongtien ?? 0
                })
                .ToListAsync();
        }

        // Phiếu thu / chi tiền mặt ghi tay trong 1 ngày (bồi thường, chi tiêu vặt). maCaDangMo: để biết khoản nào còn xóa được.
        public async Task<List<ThuChiTrongNgay>> ThuChiTrongNgayAsync(DateTime ngay, string? maCaDangMo)
        {
            var tu = ngay.Date;
            var den = tu.AddDays(1);
            var ds = await _context.GiaoDichKets.AsNoTracking()
                .Where(g => (g.Loai == LoaiGiaoDichKet.ChiTieu || g.Loai == LoaiGiaoDichKet.ThuBoiThuong)
                         && g.Thoigian >= tu && g.Thoigian < den)
                .OrderByDescending(g => g.Thoigian).ThenByDescending(g => g.Magd)
                .ToListAsync();
            var maNVs = ds.Where(g => g.Manv != null).Select(g => g.Manv!.Trim()).Distinct().ToList();
            var tenNV = await _context.NhanViens.AsNoTracking()
                .Where(n => maNVs.Contains(n.Manv))
                .Select(n => new { n.Manv, n.Hoten })
                .ToListAsync();
            return ds.Select(g => new ThuChiTrongNgay
            {
                MaGD = g.Magd,
                MaCa = g.Maca.Trim(),
                Loai = g.Loai.Trim(),
                LaThu = g.LaTienVao,
                SoTien = g.Sotien,
                NoiDung = g.Noidung,
                MaChungTu = g.Machungtu,
                ThoiGian = g.Thoigian,
                TenNV = tenNV.FirstOrDefault(n => n.Manv.Trim() == g.Manv?.Trim())?.Hoten,
                XoaDuoc = maCaDangMo != null && g.Maca.Trim() == maCaDangMo.Trim()
                          && (g.Loai.Trim() == LoaiGiaoDichKet.ChiTieu || string.IsNullOrWhiteSpace(g.Machungtu)),
            }).ToList();
        }

        public async Task<TongHopQuy> TongHopAsync(DateTime tu, DateTime den)
        {
            var dl = await DuLieuAsync(tu, den);
            var kq = new TongHopQuy();
            dl.DienVao(kq, tu, den);
            return kq;
        }

        // Đọc 1 lần toàn bộ hóa đơn / nhập kho / xuất kho / chi vặt trong khoảng [tu, den)
        public async Task<DuLieuKhoangQuy> DuLieuAsync(DateTime tu, DateTime den)
        {
            var dl = new DuLieuKhoangQuy();

            dl.HoaDon = (await HoaDonTinhDoanhThu()
                    .Where(h => h.Ngaylap >= tu && h.Ngaylap < den)
                    .Select(h => new { h.Ngaylap, h.Tongtien, h.Hinhthuc })
                    .ToListAsync())
                .Select(h => (Tg: h.Ngaylap!.Value, Tien: (long)(h.Tongtien ?? 0), TienMat: HinhThucThanhToan.LaTienMat(h.Hinhthuc)))
                .ToList();

            dl.NhapKho = (await _context.PhieuNhapKhos.AsNoTracking()
                    .Where(p => p.Ngaynk >= tu && p.Ngaynk < den)
                    .Select(p => new { p.Ngaynk, p.Tongtien })
                    .ToListAsync())
                .Select(p => (Tg: p.Ngaynk!.Value, Tien: (long)(p.Tongtien ?? 0)))
                .ToList();

            dl.XuatKho = (await _context.PhieuXuatKhos.AsNoTracking()
                    .Where(p => p.Ngayxk >= tu && p.Ngayxk < den)
                    .Select(p => new { p.Ngayxk, p.Tongtien })
                    .ToListAsync())
                .Select(p => (Tg: p.Ngayxk!.Value, Tien: (long)(p.Tongtien ?? 0)))
                .ToList();

            // Chi tiêu vặt trong các ca (trả nhập kho bằng tiền mặt đã nằm trong tiền nhập kho nên không cộng lại)
            // + thu bồi thường ghi riêng (không có mã chứng từ = không gắn hóa đơn)
            try
            {
                var gd = await _context.GiaoDichKets.AsNoTracking()
                    .Where(g => (g.Loai == LoaiGiaoDichKet.ChiTieu || g.Loai == LoaiGiaoDichKet.ThuBoiThuong)
                             && g.Thoigian >= tu && g.Thoigian < den)
                    .Select(g => new { g.Loai, g.Thoigian, g.Sotien, g.Machungtu })
                    .ToListAsync();
                dl.ChiVat = gd.Where(g => g.Loai.Trim() == LoaiGiaoDichKet.ChiTieu)
                    .Select(g => (Tg: g.Thoigian, Tien: (long)g.Sotien)).ToList();
                dl.ThuKhac = gd.Where(g => g.Loai.Trim() == LoaiGiaoDichKet.ThuBoiThuong && string.IsNullOrWhiteSpace(g.Machungtu))
                    .Select(g => (Tg: g.Thoigian, Tien: (long)g.Sotien)).ToList();
            }
            catch (Exception ex)
            {
                dl.Loi = "Chưa đọc được dữ liệu thu / chi trong ca: " + ex.Message;
            }
            return dl;
        }

        // 12 tháng của một năm
        public static List<DongThangQuy> TheoThang(DuLieuKhoangQuy dl, int nam)
            => Enumerable.Range(1, 12).Select(m =>
            {
                var tu = new DateTime(nam, m, 1);
                var dong = new DongThangQuy { Thang = m };
                dl.DienVao(dong, tu, tu.AddMonths(1));
                return dong;
            }).ToList();
    }

    public class DuLieuKhoangQuy
    {
        public List<(DateTime Tg, long Tien, bool TienMat)> HoaDon { get; set; } = new();
        public List<(DateTime Tg, long Tien)> NhapKho { get; set; } = new();
        public List<(DateTime Tg, long Tien)> XuatKho { get; set; } = new();
        public List<(DateTime Tg, long Tien)> ChiVat { get; set; } = new();
        public List<(DateTime Tg, long Tien)> ThuKhac { get; set; } = new();
        public string? Loi { get; set; }

        public void DienVao(TongHopQuy o, DateTime tu, DateTime den)
        {
            bool Trong(DateTime x) => x >= tu && x < den;
            var hd = HoaDon.Where(h => Trong(h.Tg)).ToList();
            o.SoHoaDon = hd.Count;
            o.DoanhThuTienMat = hd.Where(h => h.TienMat).Sum(h => h.Tien);
            o.DoanhThuChuyenKhoan = hd.Where(h => !h.TienMat).Sum(h => h.Tien);
            o.NhapKho = NhapKho.Where(p => Trong(p.Tg)).Sum(p => p.Tien);
            o.XuatKho = XuatKho.Where(p => Trong(p.Tg)).Sum(p => p.Tien);
            o.ChiVat = ChiVat.Where(c => Trong(c.Tg)).Sum(c => c.Tien);
            o.ThuKhac = ThuKhac.Where(c => Trong(c.Tg)).Sum(c => c.Tien);
        }
    }
}
