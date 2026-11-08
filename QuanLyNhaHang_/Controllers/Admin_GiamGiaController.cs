using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;
using System.Security.Cryptography;

namespace QuanLyNhaHang_.Controllers
{
    // Mã giảm giá (khuyến mại):
    //  - Nhân viên: CHỈ XEM danh sách mã còn hiệu lực (để áp cho khách khi thanh toán).
    //  - Quản lý (admin): xem (cả mã đã hết hạn), thêm, sửa, xóa - các action có [ChiQuanLy].
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class Admin_GiamGiaController : Controller
    {
        private readonly DbQlNhaHangContext _context;

        public Admin_GiamGiaController(DbQlNhaHangContext context)
        {
            _context = context;
        }

        // 1. Danh sách mã giảm giá. Nhân viên: chỉ mã còn hiệu lực. Quản lý: ?tatCa=true xem cả mã đã hết hạn / hết lượt.
        public async Task<IActionResult> HienThiMaGiamGia(bool tatCa = false)
        {
            bool laQuanLy = HttpContext.LaQuanLy();
            tatCa = tatCa && laQuanLy;
            var homNay = DateTime.Today;
            var q = _context.MaGiamGia.AsNoTracking();
            if (!tatCa) q = q.Where(g => g.NgayKT >= homNay && g.SoLuong > 0);
            // Sắp xếp trong bộ nhớ: mã còn hiệu lực lên trước, rồi theo ngày kết thúc
            var listMA = (await q.ToListAsync())
                .OrderByDescending(g => g.NgayKT >= homNay && g.SoLuong > 0)
                .ThenBy(g => g.NgayKT)
                .ToList();

            ViewBag.LaQuanLy = laQuanLy;
            ViewBag.TatCa = tatCa;
            return View(listMA);
        }

        // 2. Thêm mã (chỉ quản lý)
        [ChiQuanLy]
        public IActionResult TaoMaGiamGia()
        {
            return View(new MaGiamGia { NgayBD = DateTime.Today, NgayKT = DateTime.Today.AddDays(14), SoLuong = 1, PhanTram = 10 });
        }

        // 3. POST: Lưu mã giảm giá (chỉ quản lý)
        [ChiQuanLy]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoMaGiamGia(MaGiamGia model)
        {
            // Mã có thể để trống (tự sinh) nên không bắt buộc từ form
            ModelState.Remove(nameof(MaGiamGia.MaGiamGia_Code));

            if (model.NgayKT.Date < model.NgayBD.Date)
                ModelState.AddModelError(nameof(MaGiamGia.NgayKT), "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
            if (model.SoLuong < 1)
                ModelState.AddModelError(nameof(MaGiamGia.SoLuong), "Số lượng phải từ 1 trở lên.");
            // Bắt buộc nhập phần trăm giảm; SoTien = mức giảm tối đa (0 = không giới hạn)
            if (model.PhanTram < 1 || model.PhanTram > 100)
                ModelState.AddModelError(nameof(MaGiamGia.PhanTram), "Phần trăm giảm phải từ 1 đến 100.");
            if (model.SoTien < 0)
                ModelState.AddModelError(nameof(MaGiamGia.SoTien), "Mức giảm tối đa không được âm (để 0 nếu không giới hạn).");

            var code = model.MaGiamGia_Code?.Trim();
            if (string.IsNullOrEmpty(code))
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                code = RandomNumberGenerator.GetString(chars, 10);
            }
            else if (await _context.MaGiamGia.AnyAsync(g => g.MaGiamGia_Code == code))
            {
                ModelState.AddModelError(nameof(MaGiamGia.MaGiamGia_Code), "Mã này đã tồn tại.");
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                model.MaGiamGia_Code = code;
                model.NgayBD = model.NgayBD.Date;
                model.NgayKT = model.NgayKT.Date;
                _context.MaGiamGia.Add(model);
                await _context.SaveChangesAsync();

                TempData["SignSuccess"] = $"Đã tạo mã giảm giá {code} - {model.MoTa}.";
                return RedirectToAction(nameof(HienThiMaGiamGia));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu vào cơ sở dữ liệu (đã chạy Database/NangCap_Dot7_DonTrucTuyen_HuyMon.sql chưa?).");
                return View(model);
            }
        }
        // 4. Sửa mã (chỉ quản lý). Không đổi được chuỗi mã vì hóa đơn cũ đang tham chiếu tới mã.
        [ChiQuanLy]
        public async Task<IActionResult> SuaMaGiamGia(string ma)
        {
            ma = ma?.Trim() ?? "";
            var g = ma == "" ? null : await _context.MaGiamGia.AsNoTracking().FirstOrDefaultAsync(x => x.MaGiamGia_Code == ma);
            if (g == null)
            {
                TempData["SignError"] = "Không tìm thấy mã giảm giá.";
                return RedirectToAction(nameof(HienThiMaGiamGia));
            }
            return View(g);
        }

        [ChiQuanLy]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaMaGiamGia(MaGiamGia model)
        {
            var ma = model.MaGiamGia_Code?.Trim() ?? "";
            var g = ma == "" ? null : await _context.MaGiamGia.FirstOrDefaultAsync(x => x.MaGiamGia_Code == ma);
            if (g == null)
            {
                TempData["SignError"] = "Không tìm thấy mã giảm giá (có thể đã bị xóa).";
                return RedirectToAction(nameof(HienThiMaGiamGia));
            }

            if (model.NgayKT.Date < model.NgayBD.Date)
                ModelState.AddModelError(nameof(MaGiamGia.NgayKT), "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
            if (model.SoLuong < 0)
                ModelState.AddModelError(nameof(MaGiamGia.SoLuong), "Số lượng không được âm (để 0 = hết lượt dùng).");
            if (model.SoTien < 0)
                ModelState.AddModelError(nameof(MaGiamGia.SoTien), "Số tiền không được âm.");
            // Mã thường: 1-100%. Mã khách đổi điểm (phần trăm = 0): giảm cố định, cần số tiền > 0.
            if (g.PhanTram > 0 && (model.PhanTram < 1 || model.PhanTram > 100))
                ModelState.AddModelError(nameof(MaGiamGia.PhanTram), "Phần trăm giảm phải từ 1 đến 100.");
            if (g.PhanTram == 0)
            {
                model.PhanTram = 0;
                if (model.SoTien < 1)
                    ModelState.AddModelError(nameof(MaGiamGia.SoTien), "Mã đổi điểm giảm số tiền cố định, nhập số tiền lớn hơn 0.");
            }

            if (!ModelState.IsValid)
            {
                model.MaGiamGia_Code = g.MaGiamGia_Code;
                if (g.PhanTram > 0 && model.PhanTram < 1) model.PhanTram = g.PhanTram;   // giữ đúng loại mã trên form
                return View(model);
            }

            g.NgayBD = model.NgayBD.Date;
            g.NgayKT = model.NgayKT.Date;
            g.SoLuong = model.SoLuong;
            g.PhanTram = model.PhanTram;
            g.SoTien = model.SoTien;
            try
            {
                await _context.SaveChangesAsync();
                TempData["SignSuccess"] = $"Đã cập nhật mã {g.MaGiamGia_Code} - {g.MoTa}.";
                return RedirectToAction(nameof(HienThiMaGiamGia));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Không lưu được: " + (ex.InnerException?.Message ?? ex.Message));
                return View(model);
            }
        }

        // 5. Xóa mã (chỉ quản lý)
        [ChiQuanLy]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaMaGiamGia(string ma)
        {
            ma = ma?.Trim() ?? "";
            var g = ma == "" ? null : await _context.MaGiamGia.FirstOrDefaultAsync(x => x.MaGiamGia_Code == ma);
            if (g == null)
            {
                TempData["SignError"] = "Không tìm thấy mã giảm giá (có thể đã bị xóa).";
                return RedirectToAction(nameof(HienThiMaGiamGia));
            }

            bool daDung = await _context.HoaDons.AnyAsync(h => h.Magiamgia == ma);
            if (daDung)
            {
                g.SoLuong = 0;
                g.NgayKT = DateTime.Today.AddDays(-1);
                if (g.NgayBD > g.NgayKT) g.NgayBD = g.NgayKT;
                await _context.SaveChangesAsync();
                TempData["SignSuccess"] = $"Mã {ma} đã được dùng trong hóa đơn nên được ngừng sử dụng (giữ lại để tra cứu hóa đơn cũ).";
            }
            else
            {
                _context.MaGiamGia.Remove(g);
                await _context.SaveChangesAsync();
                TempData["SignSuccess"] = $"Đã xóa mã giảm giá {ma}.";
            }
            return RedirectToAction(nameof(HienThiMaGiamGia));
        }
    }
}
