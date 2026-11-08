using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;
using System.Text.RegularExpressions;

namespace QuanLyNhaHang_.Controllers
{
    public class DatBanController : Controller
    {
        private readonly DbQlNhaHangContext _context;
        private readonly XuLyDatBan _xuLyDatBan;

        public DatBanController(DbQlNhaHangContext context, XuLyDatBan xuLyDatBan)
        {
            _context = context;
            _xuLyDatBan = xuLyDatBan;
        }

        // GET: DatBan
        [HttpGet]
        public IActionResult DatBan1()
        {
            // Khách đã đăng nhập thì điền sẵn thông tin liên hệ
            return View(new PhieuDatBan
            {
                Tenkh = HttpContext.Session.GetString("HoTen"),
                Sdt = HttpContext.Session.GetString("SoDT"),
                Email = HttpContext.Session.GetString("Mail")
            });
        }

        // POST: DatBan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatBan1(PhieuDatBan model)
        {
            // Maphieu là string non-nullable nên MVC tự coi là [Required] -> ModelState luôn invalid.
            // Mã phiếu do server sinh, không nhận từ form.
            ModelState.Remove(nameof(PhieuDatBan.Maphieu));
            ModelState.Remove(nameof(PhieuDatBan.Makh));

            model.Tenkh = model.Tenkh?.Trim();
            model.Email = model.Email?.Trim();
            model.Sdt = model.Sdt?.Trim();

            if (string.IsNullOrWhiteSpace(model.Tenkh))
                ModelState.AddModelError(nameof(model.Tenkh), "Vui lòng nhập tên khách hàng.");

            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError(nameof(model.Email), "Vui lòng nhập email.");

            if (string.IsNullOrWhiteSpace(model.Sdt) || !Regex.IsMatch(model.Sdt, @"^\d{9,11}$"))
                ModelState.AddModelError(nameof(model.Sdt), "Số điện thoại phải gồm 9-11 chữ số.");

            if (model.Soluong is null or < 1 or > 20)
                ModelState.AddModelError(nameof(model.Soluong), "Vui lòng chọn số người (1-20).");

            if (!TimeSpan.TryParse(model.Giodat, out var gio))
                ModelState.AddModelError(nameof(model.Giodat), "Vui lòng chọn giờ đặt.");

            if (model.Ngaydat is null)
            {
                ModelState.AddModelError(nameof(model.Ngaydat), "Vui lòng chọn ngày đặt.");
            }
            else if (model.Ngaydat.Value.Date.Add(gio) <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(model.Ngaydat), "Vui lòng chọn ngày và giờ trong tương lai.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var loi = await LuuPhieuDatBanAsync(model);
            if (loi != null)
            {
                ModelState.AddModelError("", loi);
                return View(model);
            }

            // Gửi mail xác nhận: lỗi gửi mail không làm hỏng việc đặt bàn (đã lưu thành công)
            await _xuLyDatBan.GuiMailAsync(model.Email, model.Tenkh, model.Ngaydat, model.Sdt, model.Soluong, model.Giodat);

            TempData["ThongBao"] = $"Đặt bàn thành công! Chúng tôi đã ghi nhận phiếu {model.Maphieu} và gửi email xác nhận.";
            int tmp = HttpContext.Session.GetString("Cart") == null ? 0 : -1;
            return RedirectToAction("Welcome", "Account", new { id = -1, cart = tmp });
        }

        // Lưu phiếu đặt bàn. Trả về null nếu thành công, ngược lại trả về thông báo lỗi.
        private async Task<string?> LuuPhieuDatBanAsync(PhieuDatBan phieu)
        {
            try
            {
                phieu.Maphieu = await GeneratePhieuDatBanAsync();

                // Mã KH lấy từ Session (khách chưa đăng nhập thì để trống)
                var maKH = HttpContext.Session.GetString("MaKH")?.Trim();
                phieu.Makh = string.IsNullOrEmpty(maKH) ? null : maKH;

                // Phiếu mới luôn ở trạng thái chờ xếp bàn (không nhận từ form)
                phieu.Trangthai = TrangThaiDatBan.ChoXepBan;
                phieu.Maban = null;

                _context.PhieuDatBans.Add(phieu);
                await _context.SaveChangesAsync();
                return null;
            }
            catch (DbUpdateException ex)
            {
                return "Không lưu được phiếu đặt bàn: " + (ex.InnerException?.Message ?? ex.Message);
            }
        }

        // Sinh mã PDB0001, PDB0002, ... (lấy số lớn nhất hiện có + 1; không so sánh chuỗi)
        private async Task<string> GeneratePhieuDatBanAsync()
        {
            const string prefix = "PDB";

            var cacMa = await _context.PhieuDatBans
                .AsNoTracking()
                .Select(p => p.Maphieu)
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
    }
}
