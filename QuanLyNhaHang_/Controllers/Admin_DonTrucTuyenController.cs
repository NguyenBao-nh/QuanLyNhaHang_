using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    /// "Người mua trực tuyến": hóa đơn của khách đã đăng nhập, đặt món và thanh toán trên web.
    /// Quy trình: Chờ xác nhận -> (Xác nhận đơn) Đã xác nhận -> (Xác nhận giao) Đã giao. Có thể hủy trước khi giao.

    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class Admin_DonTrucTuyenController : Controller
    {
        private readonly DbQlNhaHangContext _context;
        private readonly XuLyMaGiamGia _xuLyMaGiamGia;
        private readonly XuLyKet _xuLyKet;

        public Admin_DonTrucTuyenController(DbQlNhaHangContext context, XuLyMaGiamGia xuLyMaGiamGia, XuLyKet xuLyKet)
        {
            _context = context;
            _xuLyMaGiamGia = xuLyMaGiamGia;
            _xuLyKet = xuLyKet;
        }

        private IQueryable<HoaDon> DonTrucTuyen() =>
            _context.HoaDons.Where(h => h.Loaidon == LoaiDon.TrucTuyen);

        private static readonly string[] _cacTab =
            { TrangThaiDon.ChoXacNhan, TrangThaiDon.DaXacNhan, TrangThaiDon.DaGiao, TrangThaiDon.DaHuy, "TatCa" };

        // GET: Admin_DonTrucTuyen/DanhSach?tt=Chờ xác nhận
        public async Task<IActionResult> DanhSach(string? tt = null)
        {
            tt = _cacTab.Contains(tt) ? tt : TrangThaiDon.ChoXacNhan;

            var q = DonTrucTuyen().AsNoTracking()
                .Include(h => h.MakhNavigation).ThenInclude(k => k!.MataikhoanNavigation)
                .Include(h => h.ManvNavigation)
                .AsQueryable();
            if (tt != "TatCa") q = q.Where(h => h.Trangthai == tt);

            // Chờ xác nhận: đơn cũ nhất lên đầu (xử lý trước); các tab khác: mới nhất lên đầu
            var ds = tt == TrangThaiDon.ChoXacNhan
                ? await q.OrderBy(h => h.Ngaylap).ToListAsync()
                : await q.OrderByDescending(h => h.Ngaylap).ToListAsync();

            // Số đơn từng trạng thái 
            var dem = await DonTrucTuyen().AsNoTracking()
                .GroupBy(h => h.Trangthai)
                .Select(g => new { TT = g.Key, SL = g.Count() })
                .ToListAsync();
            ViewBag.Dem = dem.Where(d => d.TT != null)
                .GroupBy(d => d.TT!.Trim())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.SL));
            ViewBag.TongDon = dem.Sum(d => d.SL);

            // Số món mỗi đơn
            var maHDs = ds.Select(h => h.Mahoadon).ToList();
            ViewBag.SoMon = (await _context.ChiTietHoaDons.AsNoTracking()
                    .Where(c => maHDs.Contains(c.Mahoadon))
                    .Select(c => new { c.Mahoadon, c.Soluong })
                    .ToListAsync())
                .GroupBy(c => c.Mahoadon.Trim())
                .ToDictionary(g => g.Key, g => g.Sum(c => c.Soluong ?? 0));

            ViewBag.Tab = tt;
            return View(ds);
        }

        public async Task<IActionResult> ChiTiet(string ma, string? tt = null)
        {
            ma = ma?.Trim() ?? "";
            var hd = await DonTrucTuyen().AsNoTracking()
                .Include(h => h.MakhNavigation).ThenInclude(k => k!.MataikhoanNavigation)
                .Include(h => h.ManvNavigation)
                .FirstOrDefaultAsync(h => h.Mahoadon == ma);
            if (hd == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn trực tuyến.";
                return RedirectToAction(nameof(DanhSach), new { tt });
            }

            var ct = await _context.ChiTietHoaDons.AsNoTracking()
                .Include(c => c.MamonanNavigation)
                .Where(c => c.Mahoadon == ma)
                .ToListAsync();

            ViewBag.HoaDon = hd;
            ViewBag.Tab = tt;
            ViewBag.Tong = ct.Sum(c => c.Thanhtien ?? 0);
            ViewBag.Email = hd.MakhNavigation?.MataikhoanNavigation?.Email;
            return View(ct);
        }

        // POST: xác nhận đơn (Chờ xác nhận -> Đã xác nhận). Nhân viên xác nhận được ghi vào hóa đơn.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanDon(string ma, string? tt = null, bool veChiTiet = false)
        {
            var hd = await LayDonAsync(ma);
            if (hd == null) return VeTrang(ma, tt, veChiTiet);

            if (hd.Trangthai?.Trim() != TrangThaiDon.ChoXacNhan)
            {
                TempData["ErrorMessage"] = $"Đơn {hd.Mahoadon.Trim()} đang ở trạng thái \"{hd.Trangthai}\", không thể xác nhận đơn.";
                return VeTrang(ma, tt, veChiTiet);
            }

            // Xác nhận đơn = nhà hàng đã kiểm tra tài khoản và nhận đủ tiền -> lúc này mới cộng điểm cho khách
            hd.Trangthai = TrangThaiDon.DaXacNhan;
            hd.Tgxacnhan = DateTime.Now;
            // Doanh thu chuyển khoản của đơn được tính vào ca đang mở (không cộng vào két tiền mặt)
            var ca = await _xuLyKet.LayCaDangMoAsync();
            if (ca != null) hd.Maca = ca.Maca;
            await GanNhanVienAsync(hd);
            int diem = await CongTruDiemAsync(hd, +1);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xác nhận nhận đủ {(hd.Tongtien ?? 0):N0}đ và xác nhận đơn {hd.Mahoadon.Trim()}"
                + (diem > 0 ? $" (cộng {diem:N0} điểm cho khách)" : "")
                + ". Khi món đã giao cho khách, bấm \"Xác nhận giao\".";
            return VeTrang(ma, tt, veChiTiet);
        }

        // POST: xác nhận đã giao (Đã xác nhận -> Đã giao)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanGiao(string ma, string? tt = null, bool veChiTiet = false)
        {
            var hd = await LayDonAsync(ma);
            if (hd == null) return VeTrang(ma, tt, veChiTiet);

            if (hd.Trangthai?.Trim() != TrangThaiDon.DaXacNhan)
            {
                TempData["ErrorMessage"] = hd.Trangthai?.Trim() == TrangThaiDon.ChoXacNhan
                    ? $"Đơn {hd.Mahoadon.Trim()} chưa được xác nhận. Hãy bấm \"Xác nhận đơn\" trước."
                    : $"Đơn {hd.Mahoadon.Trim()} đang ở trạng thái \"{hd.Trangthai}\", không thể xác nhận giao.";
                return VeTrang(ma, tt, veChiTiet);
            }

            hd.Trangthai = TrangThaiDon.DaGiao;
            hd.Tggiao = DateTime.Now;
            await GanNhanVienAsync(hd);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đơn {hd.Mahoadon.Trim()} đã giao xong.";
            return VeTrang(ma, tt, veChiTiet);
        }

        // POST: hủy đơn (chưa giao). Trừ lại điểm thành viên đã cộng và trả lại lượt dùng mã giảm giá.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDon(string ma, string? LyDo, string? tt = null, bool veChiTiet = false)
        {
            var hd = await LayDonAsync(ma);
            if (hd == null) return VeTrang(ma, tt, veChiTiet);

            var ttHienTai = hd.Trangthai?.Trim();
            if (ttHienTai != TrangThaiDon.ChoXacNhan && ttHienTai != TrangThaiDon.DaXacNhan)
            {
                TempData["ErrorMessage"] = $"Đơn {hd.Mahoadon.Trim()} đang ở trạng thái \"{hd.Trangthai}\", không thể hủy.";
                return VeTrang(ma, tt, veChiTiet);
            }

            LyDo = LyDo?.Trim();
            if (string.IsNullOrEmpty(LyDo))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy đơn.";
                return VeTrang(ma, tt, veChiTiet);
            }
            if (LyDo.Length > 200) LyDo = LyDo[..200];

            await using var tx = await _context.Database.BeginTransactionAsync();

            hd.Trangthai = TrangThaiDon.DaHuy;
            hd.Lydohuy = LyDo;
            await GanNhanVienAsync(hd);

            // Điểm chỉ được cộng khi đã xác nhận đơn (đã nhận tiền) -> hủy sau bước đó thì trừ lại
            bool daNhanTien = ttHienTai == TrangThaiDon.DaXacNhan;
            if (daNhanTien) await CongTruDiemAsync(hd, -1);
            await _context.SaveChangesAsync();
            await _xuLyMaGiamGia.TraLaiSoLuongAsync(hd.Magiamgia);
            await tx.CommitAsync();

            TempData["SuccessMessage"] = $"Đã hủy đơn {hd.Mahoadon.Trim()}. "
                + (daNhanTien
                    ? $"Nhà hàng đã nhận {(hd.Tongtien ?? 0):N0}đ của khách - nhớ hoàn tiền cho khách."
                    : "Nếu khách thực sự đã chuyển khoản thì cần hoàn tiền cho khách.");
            return VeTrang(ma, tt, veChiTiet);
        }


        private async Task<HoaDon?> LayDonAsync(string? ma)
        {
            ma = ma?.Trim() ?? "";
            var hd = ma == "" ? null : await DonTrucTuyen().FirstOrDefaultAsync(h => h.Mahoadon == ma);
            if (hd == null) TempData["ErrorMessage"] = "Không tìm thấy đơn trực tuyến.";
            return hd;
        }

        private async Task<int> CongTruDiemAsync(HoaDon hd, int dau)
        {
            if (string.IsNullOrWhiteSpace(hd.Makh)) return 0;
            var kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Makh == hd.Makh);
            if (kh == null) return 0;
            int diem = (int)((hd.Tongtien ?? 0) * 0.01);
            kh.Diemthanhvien = Math.Max(0, (kh.Diemthanhvien ?? 0) + dau * diem);
            return diem;
        }

        // Ghi nhân viên xử lý vào hóa đơn 
        private async Task GanNhanVienAsync(HoaDon hd)
        {
            if (!string.IsNullOrWhiteSpace(hd.Manv)) return;
            var maNV = HttpContext.Session.GetString("MaNV")?.Trim();
            if (!string.IsNullOrEmpty(maNV) && await _context.NhanViens.AnyAsync(n => n.Manv == maNV))
                hd.Manv = maNV;
        }

        private IActionResult VeTrang(string? ma, string? tt, bool veChiTiet)
            => veChiTiet && !string.IsNullOrWhiteSpace(ma)
                ? RedirectToAction(nameof(ChiTiet), new { ma = ma.Trim(), tt })
                : RedirectToAction(nameof(DanhSach), new { tt });
    }
}
