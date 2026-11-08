using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;

namespace QuanLyNhaHang_.Services
{
    public class KetQuaTaoPhieuXuatKho
    {
        public List<string> Loi { get; } = new();
        public bool ThanhCong => Loi.Count == 0;
        public string? MaPhieu { get; set; }
        public long TongTien { get; set; }
    }

    public class XuLyXuatKho
    {
        private readonly DbQlNhaHangContext _context;
        private readonly XuLyNguyenLieu _xuLyNguyenLieu;

        public XuLyXuatKho(DbQlNhaHangContext context, XuLyNguyenLieu xuLyNguyenLieu)
        {
            _context = context;
            _xuLyNguyenLieu = xuLyNguyenLieu;
        }

        // Danh sách phiếu xuất kho (mới nhất trước)
        public async Task<List<PhieuXuatKho>> LayThongTinXuatKhoAsync()
        {
            return await _context.PhieuXuatKhos
                .AsNoTracking()
                .OrderByDescending(p => p.Ngayxk)
                .ToListAsync();
        }

        // Sinh mã phiếu xuất: PXK0001, PXK0002, ... (lấy số lớn nhất hiện có + 1)
        public async Task<string> SinhMaPhieuXuatKhoAsync()
        {
            const string prefix = "PXK";

            var cacMa = await _context.PhieuXuatKhos
                .AsNoTracking()
                .Select(p => p.Maxuatkho)
                .ToListAsync();

            int max = 0;
            foreach (var ma in cacMa)
            {
                var m = ma?.Trim();
                if (!string.IsNullOrEmpty(m) &&
                    m.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(m.Substring(prefix.Length), out int so) &&
                    so > max)
                {
                    max = so;
                }
            }
            return prefix + (max + 1).ToString("D4");
        }

        // Tạo phiếu xuất kho + chi tiết trong MỘT lần lưu (SaveChanges chạy trong 1 transaction).
        // - Gộp các dòng trùng nguyên liệu (khóa chính của ChiTietXuatKho là Maxuatkho + Manguyenlieu)
        // - Đơn giá lấy từ DB, không tin dữ liệu client gửi lên
        // - Không cho xuất quá số lượng tồn
        public async Task<KetQuaTaoPhieuXuatKho> TaoPhieuXuatKhoAsync(string? maNvSession, ThemPhieuXuatKhoInput input)
        {
            var kq = new KetQuaTaoPhieuXuatKho();

            // 1. Nhân viên lập phiếu
            var maNv = (string.IsNullOrWhiteSpace(maNvSession) ? input.MaNV : maNvSession)?.Trim();
            if (string.IsNullOrEmpty(maNv) || !await _context.NhanViens.AnyAsync(n => n.Manv == maNv))
            {
                kq.Loi.Add("Vui lòng chọn nhân viên lập phiếu hợp lệ.");
            }

            // 2. Gộp dòng trùng, bỏ dòng trống / số lượng <= 0
            var dong = (input.ChiTiet ?? new List<ChiTietXuatKhoInput>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Manguyenlieu) && x.Soluong.GetValueOrDefault() > 0)
                .GroupBy(x => x.Manguyenlieu!.Trim())
                .Select(g => new { Ma = g.Key, SoLuong = g.Sum(x => (long)x.Soluong!.Value) })
                .ToList();

            if (dong.Count == 0)
            {
                kq.Loi.Add("Phiếu xuất kho phải có ít nhất một nguyên liệu với số lượng lớn hơn 0.");
            }

            // 3. Kiểm tra nguyên liệu, tồn kho; tính thành tiền từ đơn giá trong DB
            var chiTietMoi = new List<ChiTietXuatKho>();
            long tongTien = 0;

            if (kq.ThanhCong)
            {
                var dsMa = dong.Select(d => d.Ma).ToList();

                var nguyenLieus = (await _context.NguyenLieus
                        .AsNoTracking()
                        .Where(n => dsMa.Contains(n.Manguyenlieu))
                        .Select(n => new { n.Manguyenlieu, n.Tennguyenlieu, n.Dongia })
                        .ToListAsync())
                    .ToDictionary(n => n.Manguyenlieu.Trim());

                var ton = await _xuLyNguyenLieu.TinhTonKhoAsync(dsMa);

                foreach (var d in dong)
                {
                    if (!nguyenLieus.TryGetValue(d.Ma, out var nl))
                    {
                        kq.Loi.Add($"Không tìm thấy nguyên liệu có mã '{d.Ma}'.");
                        continue;
                    }

                    int tonHienTai = ton.TryGetValue(d.Ma, out var t) ? t : 0;
                    if (d.SoLuong > tonHienTai)
                    {
                        kq.Loi.Add($"Nguyên liệu '{nl.Tennguyenlieu}' chỉ còn tồn {tonHienTai}, không đủ để xuất {d.SoLuong}.");
                        continue;
                    }

                    long thanhTien = d.SoLuong * (nl.Dongia ?? 0);
                    tongTien += thanhTien;

                    if (d.SoLuong > int.MaxValue || thanhTien > int.MaxValue || tongTien > int.MaxValue)
                    {
                        kq.Loi.Add("Số lượng hoặc thành tiền quá lớn.");
                        break;
                    }

                    chiTietMoi.Add(new ChiTietXuatKho
                    {
                        Manguyenlieu = nl.Manguyenlieu,
                        Soluong = (int)d.SoLuong,
                        Thanhtien = (int)thanhTien
                    });
                }
            }

            if (!kq.ThanhCong) return kq;

            // 4. Tạo phiếu (mã luôn do server sinh lại để tránh trùng khi mở nhiều tab / F5)
            var maPhieu = await SinhMaPhieuXuatKhoAsync();
            foreach (var ct in chiTietMoi) ct.Maxuatkho = maPhieu;

            var phieu = new PhieuXuatKho
            {
                Maxuatkho = maPhieu,
                Manv = maNv!,
                Ngayxk = input.NgayXuatKho ?? DateTime.Now,
                Tongtien = (int)tongTien,
                ChiTietXuatKhos = chiTietMoi
            };

            try
            {
                _context.PhieuXuatKhos.Add(phieu);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                kq.Loi.Add("Không lưu được phiếu xuất kho: " + (ex.InnerException?.Message ?? ex.Message));
                return kq;
            }

            kq.MaPhieu = maPhieu;
            kq.TongTien = tongTien;
            return kq;
        }

        // Chi tiết một phiếu xuất kho
        public async Task<List<ChiTietXuatKhoViewModel>> GetChiTietXuatKhoAsync(string maXuatKho)
        {
            var ma = (maXuatKho ?? string.Empty).Trim();

            return await _context.ChiTietXuatKhos
                .AsNoTracking()
                .Where(c => c.Maxuatkho == ma)
                .Select(c => new ChiTietXuatKhoViewModel
                {
                    MaXuatKho = c.Maxuatkho.Trim(),
                    MaNguyenLieu = c.Manguyenlieu.Trim(),
                    TenNguyenLieu = c.ManguyenlieuNavigation.Tennguyenlieu,
                    SoLuong = c.Soluong ?? 0,
                    ThanhTien = c.Thanhtien ?? 0
                })
                .ToListAsync();
        }
    }
}
