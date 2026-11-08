using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;

namespace QuanLyNhaHang_.Services
{
    /// <summary>
    /// Ca làm việc và két tiền mặt.
    /// Tiền két hiện tại = tiền đầu ca + tiền mặt thu trong ca - chi tiêu - trả tiền nhập kho - tiền rút.
    /// </summary>
    public class XuLyKet
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyKet(DbQlNhaHangContext context)
        {
            _context = context;
        }

        public class TongKetCa
        {
            public int TienDauCa { get; set; }
            public int ThuTienMat { get; set; }
            public int ThuBanTienMat { get; set; }
            public int ThuBoiThuong { get; set; }
            public int ChiTieu { get; set; }
            public int TraNhapKho { get; set; }
            public int TienRut { get; set; }
            public int TongChi => ChiTieu + TraNhapKho;
            public int TienKet => TienDauCa + ThuTienMat - TongChi - TienRut;

            public int SoHoaDon { get; set; }
            public int DoanhThu { get; set; }
            public int DoanhThuTienMat { get; set; }
            public int DoanhThuChuyenKhoan { get; set; }
        }

        /// <summary>Ca đang mở (chỉ có tối đa 1 ca mở). null nếu chưa mở ca hoặc CSDL chưa nâng cấp.</summary>
        public async Task<CaLamViec?> LayCaDangMoAsync()
        {
            try
            {
                return await _context.CaLamViecs
                    .Where(c => c.Trangthai == TrangThaiCa.DangMo)
                    .OrderByDescending(c => c.Tgmo)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không đọc được ca làm việc (đã chạy NangCap_Dot9_CaLamViec_Ket.sql chưa?): " + ex.Message);
                return null;
            }
        }

        public async Task<TongKetCa> TinhAsync(CaLamViec ca)
        {
            var maCa = ca.Maca;
            var gd = await _context.GiaoDichKets.AsNoTracking()
                .Where(g => g.Maca == maCa)
                .GroupBy(g => g.Loai)
                .Select(g => new { Loai = g.Key, Tong = g.Sum(x => x.Sotien) })
                .ToListAsync();
            int Tong(string loai) => gd.Where(x => x.Loai.Trim() == loai).Sum(x => x.Tong);

            // Hóa đơn của ca (không tính đơn trực tuyến đã hủy / chưa đối soát)
            var hd = await _context.HoaDons.AsNoTracking()
                .Where(h => h.Maca == maCa && h.Trangthai != TrangThaiDon.DaHuy && h.Trangthai != TrangThaiDon.ChoXacNhan)
                .Select(h => new { h.Tongtien, h.Hinhthuc })
                .ToListAsync();

            return new TongKetCa
            {
                TienDauCa = ca.Tiendauca,
                ThuTienMat = Tong(LoaiGiaoDichKet.ThuBan) + Tong(LoaiGiaoDichKet.ThuBoiThuong),
                ThuBanTienMat = Tong(LoaiGiaoDichKet.ThuBan),
                ThuBoiThuong = Tong(LoaiGiaoDichKet.ThuBoiThuong),
                ChiTieu = Tong(LoaiGiaoDichKet.ChiTieu),
                TraNhapKho = Tong(LoaiGiaoDichKet.NhapKho),
                TienRut = Tong(LoaiGiaoDichKet.RutTien),
                SoHoaDon = hd.Count,
                DoanhThu = hd.Sum(h => h.Tongtien ?? 0),
                DoanhThuTienMat = hd.Where(h => HinhThucThanhToan.LaTienMat(h.Hinhthuc)).Sum(h => h.Tongtien ?? 0),
                DoanhThuChuyenKhoan = hd.Where(h => !HinhThucThanhToan.LaTienMat(h.Hinhthuc)).Sum(h => h.Tongtien ?? 0),
            };
        }

        /// <summary>Tiền trong két hiện tại (null nếu chưa mở ca).</summary>
        public async Task<int?> TienKetHienTaiAsync()
        {
            var ca = await LayCaDangMoAsync();
            return ca == null ? null : (await TinhAsync(ca)).TienKet;
        }

        /// <summary>Thêm 1 giao dịch két vào ca (chưa SaveChanges - gọi trong transaction của nghiệp vụ).</summary>
        public void ThemGiaoDich(CaLamViec ca, string loai, int soTien, string? noiDung, string? maChungTu, string? maNV)
        {
            if (soTien <= 0) return;
            _context.GiaoDichKets.Add(new GiaoDichKet
            {
                Maca = ca.Maca,
                Loai = loai,
                Sotien = soTien,
                Noidung = string.IsNullOrWhiteSpace(noiDung) ? null : (noiDung.Length > 300 ? noiDung[..300] : noiDung),
                Machungtu = string.IsNullOrWhiteSpace(maChungTu) ? null : maChungTu.Trim(),
                Thoigian = DateTime.Now,
                Manv = string.IsNullOrWhiteSpace(maNV) ? null : maNV.Trim()
            });
        }
    }
}
