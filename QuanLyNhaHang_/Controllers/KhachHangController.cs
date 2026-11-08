using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Extensions;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;
using System.Security.Cryptography;

namespace QuanLyNhaHang_.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly DbQlNhaHangContext _context;
        private readonly XuLyThongTinKhachHang _xuLyKhachHang;
        private readonly XuLyMaGiamGia _xuLyMaGiamGia;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] _allowedExt = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxAvatarSize = 5 * 1024 * 1024;

        public KhachHangController(DbQlNhaHangContext context, XuLyThongTinKhachHang xuLyKhachHang,
                                   XuLyMaGiamGia xuLyMaGiamGia, IWebHostEnvironment env)
        {
            _context = context;
            _xuLyKhachHang = xuLyKhachHang;
            _xuLyMaGiamGia = xuLyMaGiamGia;
            _env = env;
        }

        // Lưu avatar vào wwwroot/Images/Avatar, trả về tên file (null nếu không có / không hợp lệ)
        private async Task<string?> LuuAvatarAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0 || file.Length > MaxAvatarSize) return null;
            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExt.Contains(ext)) return null;

            string folder = Path.Combine(_env.WebRootPath, "Images", "Avatar");
            Directory.CreateDirectory(folder);
            string fileName = Guid.NewGuid().ToString("N") + ext;
            using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return fileName;
        }

        // GET: KhachHang/Profile
        public async Task<IActionResult> Profile()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("MaTaiKhoan")))
                return RedirectToAction("DangNhap", "Account");

            ViewBag.Mail = HttpContext.Session.GetString("Mail");
            ViewBag.HoTen = HttpContext.Session.GetString("HoTen");
            ViewBag.Avatar = HttpContext.Session.GetString("Avatar");
            ViewBag.MaKH = HttpContext.Session.GetString("MaKH");
            ViewBag.NgaySinh = HttpContext.Session.GetString("NgaySinh");
            ViewBag.SoDT = HttpContext.Session.GetString("SoDT");
            ViewBag.DiemThanhVien = HttpContext.Session.GetInt32("DiemThanhVien") ?? 0;
            ViewBag.Cart = HttpContext.Session.GetObjectFromJson<List<GioHang>>("Cart")?.Count ?? 0;

            // View khai báo @model IEnumerable<HoaDon> (lịch sử đơn hàng) nên PHẢI truyền model, nếu không sẽ lỗi 500
            var maKH = HttpContext.Session.GetString("MaKH");
            var hoaDons = string.IsNullOrEmpty(maKH)
                ? new List<HoaDon>()
                : (await _xuLyKhachHang.HoaDonTheoMaKhachAsync(maKH)).OrderByDescending(h => h.Ngaylap).ToList();
            return View(hoaDons);
        }

        // GET: KhachHang/NhapThongTinKhachHang
        public IActionResult NhapThongTinKhachHang()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("MaTaiKhoan")))
                return RedirectToAction("DangNhap", "Account");
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("MaKH")))
                return RedirectToAction(nameof(Profile));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NhapThongTinKhachHang(KhachHang model, IFormFile? avatarFile)
        {
            // Mã KH, mã tài khoản do server gán; navigation không đến từ form
            ModelState.Remove(nameof(KhachHang.Makh));
            ModelState.Remove(nameof(KhachHang.Mataikhoan));
            ModelState.Remove(nameof(KhachHang.MataikhoanNavigation));

            var maTaiKhoan = HttpContext.Session.GetString("MaTaiKhoan");
            if (string.IsNullOrEmpty(maTaiKhoan))
            {
                TempData["ErrorMessage"] = true;
                return RedirectToAction("DangNhap", "Account");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = true;
                return View(model);
            }

            try
            {
                model.Makh = await SinhMaHelper.TaoMaTiepTheoAsync(_context.KhachHangs.Select(k => k.Makh), "KH");
                model.Mataikhoan = maTaiKhoan;
                model.Diemthanhvien ??= 0;
                model.Avatar = await LuuAvatarAsync(avatarFile);   // bản cũ bỏ qua file avatar

                _context.KhachHangs.Add(model);
                await _context.SaveChangesAsync();

                // Đã đăng nhập sẵn -> cập nhật session để dùng giỏ hàng/thanh toán ngay
                HttpContext.Session.SetString("MaKH", model.Makh.Trim());
                HttpContext.Session.SetString("HoTen", model.Hoten ?? string.Empty);
                HttpContext.Session.SetString("SoDT", model.Sodt ?? string.Empty);
                HttpContext.Session.SetString("Avatar", model.Avatar ?? string.Empty);
                HttpContext.Session.SetString("NgaySinh", model.Ngaysinh?.ToString("dd/MM/yyyy") ?? string.Empty);
                HttpContext.Session.SetInt32("DiemThanhVien", model.Diemthanhvien ?? 0);

                TempData["ThongBao"] = "Đã lưu thông tin khách hàng. Chào mừng bạn đến với FREEDOM!";
                return RedirectToAction("Welcome", "Account");
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = true;
                return View(model);
            }
        }

        // GET: KhachHang/lichSuDonHang
        // View lichSuDonHang chỉ là phần nhúng trong trang Profile (không có layout) nên chuyển về Profile
        public IActionResult lichSuDonHang() => RedirectToAction(nameof(Profile));

        // GET: KhachHang/InHoaDon?ma=HD0001 -> khách in hóa đơn của chính mình
        public async Task<IActionResult> InHoaDon(string ma)
        {
            if (string.IsNullOrWhiteSpace(ma))
                return RedirectToAction("Welcome", "Account");

            var model = await HoaDonInViewModel.TaiAsync(_context, ma);
            if (model == null)
                return RedirectToAction("Welcome", "Account");

            var maKH = HttpContext.Session.GetString("MaKH");
            bool laNhanVien = HttpContext.Session.GetString("LoaiTK") == "NhanVien";
            if (!laNhanVien && (string.IsNullOrEmpty(maKH) || model.HoaDon.Makh?.Trim() != maKH.Trim()))
                return RedirectToAction(nameof(Profile));

            return View("~/Views/Shared/InHoaDon.cshtml", model);
        }

        // GET: KhachHang/ShowCTHD?ma=HD0001
        public async Task<IActionResult> ShowCTHD(string ma)
        {
            if (string.IsNullOrWhiteSpace(ma))
                return RedirectToAction("Welcome", "Account");

            ma = ma.Trim();
            var hoaDon = await _context.HoaDons.AsNoTracking()
                .Include(h => h.MakhNavigation)
                .FirstOrDefaultAsync(h => h.Mahoadon == ma);
            if (hoaDon == null)
                return RedirectToAction("Welcome", "Account");

            // Khách chỉ được xem hóa đơn của chính mình (nhân viên/admin không có MaKH nên xem được)
            var maKH = HttpContext.Session.GetString("MaKH");
            if (!string.IsNullOrEmpty(maKH) && hoaDon.Makh?.Trim() != maKH.Trim())
                return RedirectToAction(nameof(lichSuDonHang));

            var chiTiets = await _context.ChiTietHoaDons.AsNoTracking()
                .Include(c => c.MamonanNavigation)
                .Where(c => c.Mahoadon == ma)
                .ToListAsync();

            ViewBag.DanhGia = await _context.DanhGia.AsNoTracking().FirstOrDefaultAsync(d => d.Mahoadon == ma);
            ViewBag.HoaDon = hoaDon;
            ViewBag.Khach = hoaDon.MakhNavigation;
            ViewBag.MaHD = ma;
            ViewBag.Tong = chiTiets.Sum(t => t.Thanhtien ?? 0);
            return View(chiTiets);
        }

        // GET: KhachHang/DoiMaGiamGia
        public async Task<IActionResult> DoiMaGiamGia()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("MaKH")))
                return RedirectToAction("DangNhap", "Account");

            await NapThongTinDoiMaAsync();
            return View();
        }

        // POST: KhachHang/XuLyDoiMa  (form gửi MaGiamGia = số điểm đổi = số tiền giảm)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XuLyDoiMa(IFormCollection f)
        {
            var maKH = HttpContext.Session.GetString("MaKH");
            if (string.IsNullOrEmpty(maKH))
                return RedirectToAction("DangNhap", "Account");

            var khach = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Makh == maKH);
            if (khach == null)
                return RedirectToAction("DangNhap", "Account");

            // Cũ không kiểm tra điểm -> có thể đổi quá số điểm đang có. Bản mới kiểm tra.
            if (!int.TryParse(f["MaGiamGia"], out int soTien) || soTien <= 0 || soTien > (khach.Diemthanhvien ?? 0))
            {
                TempData["ErrorMessage"] = "Số điểm đổi không hợp lệ hoặc vượt quá điểm hiện có!";
                await NapThongTinDoiMaAsync();
                return View("DoiMaGiamGia");
            }

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string randomString = RandomNumberGenerator.GetString(chars, 10);

            DateTime ngayBD = DateTime.Today;
            DateTime ngayKT = ngayBD.AddDays(14);

            await using (var tx = await _context.Database.BeginTransactionAsync())
            {
                await _xuLyMaGiamGia.ThemAsync(randomString, ngayBD, ngayKT, 1, soTien);
                khach.Diemthanhvien = (khach.Diemthanhvien ?? 0) - soTien;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }

            int diemConLai = khach.Diemthanhvien ?? 0;
            HttpContext.Session.SetInt32("DiemThanhVien", diemConLai);

            ViewBag.RandomMaGiamGia = randomString;
            ViewBag.SoTien = soTien;
            ViewBag.NgayBD = ngayBD;
            ViewBag.NgayKT = ngayKT;

            var mail = HttpContext.Session.GetString("Mail");
            if (!string.IsNullOrEmpty(mail))
                new KetNoiTaiKhoan().GuiMailMaGiamGia(mail, randomString, soTien, ngayBD, ngayKT, diemConLai);

            await NapThongTinDoiMaAsync();
            return View("DoiMaGiamGia");
        }

        private Task NapThongTinDoiMaAsync()
        {
            ViewBag.DiemThanhVien = HttpContext.Session.GetInt32("DiemThanhVien") ?? 0;
            ViewBag.MaKH = HttpContext.Session.GetString("MaKH");
            ViewBag.Mail = HttpContext.Session.GetString("Mail");
            ViewBag.HoTen = HttpContext.Session.GetString("HoTen");
            return Task.CompletedTask;
        }
    }
}
