using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    // Quản lý hóa đơn bán hàng (HoaDon). Admin_HoaDonController chỉ lo phiếu nhập kho nên không trùng.
    // Chỉ nhân viên đã đăng nhập; mọi POST phải có mã antiforgery (form trên giao diện)
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class NhaHangController : Controller
    {
        private readonly DbQlNhaHangContext _context;

        public NhaHangController(DbQlNhaHangContext context)
        {
            _context = context;
        }

        private static string? NullNeuRong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // GET: NhaHang/ShowHD  (lọc theo khoảng ngày)
        public async Task<IActionResult> ShowHD(string? ngayBD = null, string? ngayKT = null)
        {
            var query = _context.HoaDons.AsNoTracking()
                .Include(h => h.MakhNavigation)   // thay cho cột TenKhachHang cũ: dùng item.MakhNavigation?.Hoten
                .AsQueryable();

            if (DateTime.TryParse(ngayBD, out var start) && DateTime.TryParse(ngayKT, out var end))
            {
                var endDate = end.Date.AddDays(1).AddTicks(-1); // cuối ngày kết thúc
                query = query.Where(h => h.Ngaylap >= start.Date && h.Ngaylap <= endDate);
            }

            var hoaDons = await query.OrderByDescending(h => h.Ngaylap).ToListAsync();
            ViewBag.SL = hoaDons.Count;
            return View(hoaDons);
        }

        [HttpPost]
        public IActionResult ShowHD(string ma)
        {
            if (string.IsNullOrWhiteSpace(ma))
                return RedirectToAction(nameof(Error));

            return RedirectToAction(nameof(ShowCTHD), new { ma });
        }

        // GET: NhaHang/InHoaDon?ma=HD0001  -> trang in hóa đơn (khổ giấy 80mm), ?auto=1 tự mở hộp thoại in
        public async Task<IActionResult> InHoaDon(string ma)
        {
            if (string.IsNullOrWhiteSpace(ma))
                return RedirectToAction(nameof(Error));

            var model = await HoaDonInViewModel.TaiAsync(_context, ma);
            if (model == null)
                return RedirectToAction(nameof(Error));

            return View("~/Views/Shared/InHoaDon.cshtml", model);
        }

        // GET: NhaHang/ShowCTHD?ma=HD0001  (inNgay=true: vừa thanh toán xong -> hiện nút In hóa đơn nổi bật)
        public async Task<IActionResult> ShowCTHD(string ma, bool inNgay = false)
        {
            ViewBag.VuaThanhToan = inNgay;
            if (string.IsNullOrWhiteSpace(ma))
                return RedirectToAction(nameof(Error));

            ma = ma.Trim();
            var hoaDon = await _context.HoaDons.AsNoTracking()
                .Include(h => h.MakhNavigation)
                .FirstOrDefaultAsync(h => h.Mahoadon == ma);
            if (hoaDon == null)
                return RedirectToAction(nameof(Error));

            var chiTiets = await _context.ChiTietHoaDons.AsNoTracking()
                .Include(c => c.MamonanNavigation)   // tên món / đơn giá: c.MamonanNavigation.Tenmon, .Gia
                .Where(c => c.Mahoadon == ma)
                .ToListAsync();

            ViewBag.DanhGia = await _context.DanhGia.AsNoTracking().FirstOrDefaultAsync(d => d.Mahoadon == ma);
            ViewBag.HoaDon = hoaDon;
            ViewBag.Khach = hoaDon.MakhNavigation;
            ViewBag.MaHD = ma;
            ViewBag.Tong = chiTiets.Sum(t => t.Thanhtien ?? 0);
            return View(chiTiets);
        }

        // GET: NhaHang/ThemHD
        public async Task<IActionResult> ThemHD()
        {
            ViewBag.MaHD = await SinhMaHoaDonAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemHD(string MaHD, string? MaNV, string? MaKH, DateTime NgayLap)
        {
            MaNV = NullNeuRong(MaNV);
            MaKH = NullNeuRong(MaKH);

            if (MaNV != null && !await _context.NhanViens.AnyAsync(n => n.Manv == MaNV))
            {
                TempData["ErrorMessage"] = "Mã nhân viên không tồn tại!";
                ViewBag.MaHD = MaHD;
                return View();
            }

            if (MaKH == null || !await _context.KhachHangs.AnyAsync(k => k.Makh == MaKH))
            {
                TempData["ErrorMessage"] = "Mã khách hàng không tồn tại!";
                ViewBag.MaHD = MaHD;
                return View();
            }

            if (await _context.HoaDons.AnyAsync(h => h.Mahoadon == MaHD))
                MaHD = await SinhMaHoaDonAsync();   // tránh trùng khóa chính

            try
            {
                _context.HoaDons.Add(new HoaDon
                {
                    Mahoadon = MaHD.Trim(),
                    Manv = MaNV,
                    Makh = MaKH,
                    Ngaylap = NgayLap,
                    Tongtien = 0,
                    Loaidon = LoaiDon.TaiQuan,   // hóa đơn nhân viên lập tay (không phải đơn trực tuyến)
                    Trangthai = TrangThaiDon.HoanThanh
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm hóa đơn thành công!";
                return RedirectToAction(nameof(ThemHD));
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Thêm hóa đơn thất bại!";
                ViewBag.MaHD = MaHD;
                return View();
            }
        }

        // GET: NhaHang/Edit/HD0001
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Error));

            var model = await _context.HoaDons.AsNoTracking().FirstOrDefaultAsync(h => h.Mahoadon == id.Trim());
            if (model == null)
                return RedirectToAction(nameof(Error));

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HoaDon model)
        {
            if (string.IsNullOrWhiteSpace(model.Mahoadon))
            {
                TempData["ErrorMessage"] = "Mã hóa đơn không hợp lệ!";
                return RedirectToAction(nameof(ShowHD));
            }

            // Các thuộc tính navigation/collection không đến từ form nên bỏ khỏi ModelState
            foreach (var key in ModelState.Keys.Where(k => k.Contains("Navigation") || k.StartsWith("ChiTietHoaDons") || k.StartsWith("DanhGia")).ToList())
                ModelState.Remove(key);

            if (!ModelState.IsValid)
                return View(model);

            var hd = await _context.HoaDons.FirstOrDefaultAsync(h => h.Mahoadon == model.Mahoadon.Trim());
            if (hd == null)
            {
                TempData["ErrorMessage"] = "Cập nhật hóa đơn thất bại!";
                return RedirectToAction(nameof(ShowHD));
            }

            // Chỉ cập nhật các trường được phép sửa (giống UpdateHoaDon cũ), không bind trực tiếp cả entity
            hd.Maban = NullNeuRong(model.Maban);
            hd.Makh = NullNeuRong(model.Makh);
            hd.Tenkhach = NullNeuRong(model.Tenkhach);
            hd.Sdtkhach = NullNeuRong(model.Sdtkhach);
            hd.Manv = NullNeuRong(model.Manv);
            hd.Magiamgia = NullNeuRong(model.Magiamgia);
            hd.Ngaylap = model.Ngaylap;
            hd.Hinhthuc = NullNeuRong(model.Hinhthuc);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật hóa đơn thành công!";
                return RedirectToAction(nameof(ShowHD));
            }
            catch (DbUpdateException)
            {
                // thường do sai mã bàn / khách hàng / nhân viên / giảm giá (vi phạm khóa ngoại)
                TempData["ErrorMessage"] = "Cập nhật hóa đơn thất bại!";
                return RedirectToAction(nameof(Edit), new { id = model.Mahoadon });
            }
        }

        // GET: NhaHang/ListTimKiem?txt_Search=...
        public async Task<IActionResult> ListTimKiem(string? txt_Search)
        {
            var query = _context.HoaDons.AsNoTracking().Include(h => h.MakhNavigation).AsQueryable();

            if (!string.IsNullOrWhiteSpace(txt_Search))
            {
                var s = txt_Search.Trim();
                query = query.Where(h => h.Mahoadon.Contains(s)
                                      || (h.Maban != null && h.Maban.Contains(s))
                                      || (h.Makh != null && h.Makh.Contains(s))
                                      || (h.Tenkhach != null && h.Tenkhach.Contains(s))
                                      || (h.Sdtkhach != null && h.Sdtkhach.Contains(s))
                                      || (h.MakhNavigation != null && h.MakhNavigation.Hoten != null && h.MakhNavigation.Hoten.Contains(s))
                                      || (h.Manv != null && h.Manv.Contains(s))
                                      || (h.Magiamgia != null && h.Magiamgia.Contains(s))
                                      || (h.Hinhthuc != null && h.Hinhthuc.Contains(s)));
            }

            var hoaDons = await query.ToListAsync();
            ViewBag.SL = hoaDons.Count;
            return View(hoaDons);
        }

        public IActionResult Error() => View();

        private Task<string> SinhMaHoaDonAsync()
            => SinhMaHelper.TaoMaTiepTheoAsync(_context.HoaDons.Select(h => h.Mahoadon), "HD");
    }
}
