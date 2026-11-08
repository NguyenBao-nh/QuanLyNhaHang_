using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhaHang_.Controllers
{
    public class AccountController : Controller
    {
        private readonly DbQlNhaHangContext _context;
        private readonly IConfiguration _configuration;
        private readonly KetNoiTaiKhoan _taiKhoanRepo;

        public AccountController(DbQlNhaHangContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _taiKhoanRepo = new KetNoiTaiKhoan(_configuration);
        }

        // ĐĂNG XUẤT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Welcome", "Account", new { id = -1, cart = 0 });
        }

        // XÁC THỰC & ĐĂNG KÝ
        public IActionResult XacThuc()
        {
            return View();
        }

        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangKy(TaiKhoan model)
        {
            // Các trường này không nhập từ form (Mataikhoan/Maquyen do server gán)
            ModelState.Remove(nameof(TaiKhoan.Mataikhoan));
            ModelState.Remove(nameof(TaiKhoan.Maquyen));
            ModelState.Remove(nameof(TaiKhoan.MaquyenNavigation));

            string tenDangNhap = (model.Tendangnhap ?? string.Empty).Trim();
            string matKhau = model.Matkhau ?? string.Empty;
            string email = (model.Email ?? string.Empty).Trim();

            string? loi = null;
            if (tenDangNhap == "" || matKhau == "" || email == "")
                loi = "Vui lòng nhập đầy đủ tên đăng nhập, mật khẩu và email.";
            else if (tenDangNhap.Length > 20)
                loi = "Tên đăng nhập tối đa 20 ký tự.";
            else if (matKhau.Length < 6 || matKhau.Length > 20)
                loi = "Mật khẩu phải từ 6 đến 20 ký tự.";
            else if (matKhau != model.XacNhanMatKhau)
                loi = "Mật khẩu xác nhận không khớp.";
            else if (email.Length > 40 || !new EmailAddressAttribute().IsValid(email))
                loi = "Email không hợp lệ (tối đa 40 ký tự).";

            if (loi != null)
            {
                TempData["ErrorMessage"] = loi;
                return View(model);
            }

            if (await _context.TaiKhoans.AnyAsync(t => t.Tendangnhap != null && t.Tendangnhap == tenDangNhap))
            {
                TempData["ErrorMessageSignup"] = true;
                return View(model);
            }
            if (await _context.TaiKhoans.AnyAsync(t => t.Email != null && t.Email == email))
            {
                TempData["ErrorMessage"] = "Email này đã được dùng cho một tài khoản khác.";
                return View(model);
            }

            // Mã quyền "khách hàng" lấy theo dữ liệu
            string? maQuyenKH = await LayMaQuyenKhachHangAsync();
            if (maQuyenKH == null)
            {
                TempData["ErrorMessage"] = "Không xác định được quyền 'khách hàng' trong bảng QuyenTruyCap. Hãy thêm dòng quyền khách hàng hoặc đặt Quyen:KhachHang trong appsettings.json.";
                return View(model);
            }

            TaiKhoan tk;
            string token = Guid.NewGuid().ToString();
            try
            {
                tk = new TaiKhoan
                {
                    Mataikhoan = await SinhMaHelper.TaoMaTiepTheoAsync(_context.TaiKhoans.Select(t => t.Mataikhoan), "TK"),
                    Maquyen = maQuyenKH,
                    Tendangnhap = tenDangNhap,
                    Matkhau = matKhau,
                    Email = email,
                    Isemailconfirmed = 0,
                    Emailconfirmationtoken = token
                };
                _context.TaiKhoans.Add(tk);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không lưu được tài khoản: " + (ex.InnerException?.Message ?? ex.Message);
                return View(model);
            }

            // Link xác nhận dùng đúng địa chỉ/cổng đang chạy
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string link = $"{baseUrl}/Account/ConfirmEmail?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
            bool guiDuoc = await Task.Run(() => _taiKhoanRepo.SendConfirmationEmail(email, link));

            if (guiDuoc)
            {
                TempData["SignSuccess"] = true;
                TempData["SignNext"] = "AConfirmEmail";
                TempData["SignText"] = "Vui lòng kiểm tra email để xác thực tài khoản.";
                return RedirectToAction("DangKy");
            }

            // Gửi mail lỗi (sai mật khẩu ứng dụng Gmail, mất mạng...)
            if (_configuration.GetValue<bool>("Email:TuDongXacNhanKhiGuiLoi"))
            {
                // Chế độ phát triển: kích hoạt luôn để vẫn đăng nhập thử được
                tk.Isemailconfirmed = 1;
                await _context.SaveChangesAsync();
                TempData["SignSuccess"] = true;
                TempData["SignNext"] = "DangNhap";
                TempData["SignText"] = "Không gửi được email xác thực nên tài khoản đã được kích hoạt tự động (chế độ phát triển). Bạn có thể đăng nhập ngay.";
                return RedirectToAction("DangKy");
            }

            // Không để lại tài khoản "treo" chưa xác thực mà không có cách xác thực
            _context.TaiKhoans.Remove(tk);
            await _context.SaveChangesAsync();
            TempData["ErrorMessage"] = "Không gửi được email xác thực nên chưa tạo tài khoản. Hãy kiểm tra cấu hình Gmail (mật khẩu ứng dụng) rồi thử lại.";
            return View(model);
        }

        // Xác định mã quyền "khách hàng" từ dữ liệu thật 
        // Thứ tự: cấu hình Quyen:KhachHang -> quyền có tên chứa "khách/customer" -> quyền của tài khoản đã có hồ sơ khách hàng.
        private async Task<string?> LayMaQuyenKhachHangAsync()
        {
            var cauHinh = _configuration["Quyen:KhachHang"];
            if (!string.IsNullOrWhiteSpace(cauHinh)) return cauHinh.Trim();

            var dsQuyen = await _context.QuyenTruyCaps.AsNoTracking().ToListAsync();
            foreach (var q in dsQuyen)
            {
                string ten = BoDau(q.Tenquyen);
                if (ten.Contains("khach") || ten.Contains("customer"))
                    return q.Maquyen.Trim();
            }

            var maTuKhach = await _context.TaiKhoans.AsNoTracking()
                .Where(t => t.KhachHangs.Any())
                .Select(t => t.Maquyen)
                .FirstOrDefaultAsync();
            return string.IsNullOrWhiteSpace(maTuKhach) ? null : maTuKhach.Trim();
        }

        private static string BoDau(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder();
            foreach (char c in s.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant();
        }

        public IActionResult AConfirmEmail()
        {
            return View();
        }

        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Liên kết xác nhận không hợp lệ.";
                return RedirectToAction("DangNhap");
            }

            var tk = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.Email == email && t.Emailconfirmationtoken == token && t.Isemailconfirmed == 0);

            if (tk != null)
            {
                tk.Isemailconfirmed = 1;
                await _context.SaveChangesAsync();

                TempData["Message"] = "Xác nhận email thành công!";
                return View();
            }
            else
            {
                var isAlreadyConfirmed = await _context.TaiKhoans
                    .AnyAsync(t => t.Email == email && t.Isemailconfirmed == 1);

                if (isAlreadyConfirmed)
                {
                    TempData["ErrorMessage"] = "Trạng thái xác nhận đã được cập nhật trước đó.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Liên kết xác nhận không hợp lệ.";
                }
                return RedirectToAction("DangNhap");
            }
        }

        // ĐĂNG NHẬP
        public IActionResult DangNhap()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangNhap(TaiKhoan model)
        {
            if (string.IsNullOrEmpty(model.Tendangnhap) || string.IsNullOrEmpty(model.Matkhau))
            {
                TempData["ErrorMessage"] = "Thông tin đăng nhập không chính xác.";
                return View(model);
            }

            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.KhachHangs)
                .Include(t => t.NhanViens)
                .FirstOrDefaultAsync(t => t.Tendangnhap!.ToLower() == model.Tendangnhap.ToLower() && t.Matkhau == model.Matkhau);

            if (taiKhoan == null)
            {
                TempData["ErrorMessage"] = "Thông tin đăng nhập không chính xác.";
                return View(model);
            }

            if (taiKhoan.Isemailconfirmed == 0)
            {
                TempData["ErrorMessageComfirm"] = "Tài khoản chưa được xác thực qua email.";
                return View(model);
            }

            HttpContext.Session.SetString("MaTaiKhoan", taiKhoan.Mataikhoan.Trim());
            HttpContext.Session.SetString("MaQuyen", taiKhoan.Maquyen ?? string.Empty);
            HttpContext.Session.SetString("Mail", taiKhoan.Email ?? string.Empty);

            var khachHang = taiKhoan.KhachHangs.FirstOrDefault();
            if (khachHang != null)
            {
                HttpContext.Session.SetString("HoTen", khachHang.Hoten ?? string.Empty);
                HttpContext.Session.SetString("Avatar", khachHang.Avatar ?? string.Empty);
                HttpContext.Session.SetString("MaKH", khachHang.Makh.Trim());
                HttpContext.Session.SetString("SoDT", khachHang.Sodt ?? string.Empty);
                HttpContext.Session.SetInt32("DiemThanhVien", khachHang.Diemthanhvien ?? 0);
                HttpContext.Session.SetString("NgaySinh", khachHang.Ngaysinh?.ToString("dd/MM/yyyy") ?? string.Empty);
            }

            var nhanVien = taiKhoan.NhanViens.FirstOrDefault();
            if (nhanVien != null)
            {
                HttpContext.Session.SetString("HoTen", nhanVien.Hoten ?? string.Empty);
                HttpContext.Session.SetString("MaNV", nhanVien.Manv.Trim());
                HttpContext.Session.SetString("SoDT", nhanVien.Sodt ?? string.Empty);
            }

            // Phân loại người dùng 
            //  - có hồ sơ NhanVien  -> nhân viên / quản trị
            //  - có hồ sơ KhachHang -> khách hàng
            //  - chưa có hồ sơ nào -> dựa vào mã quyền khách hàng lấy từ CSDL
            string maQuyen = (taiKhoan.Maquyen ?? string.Empty).Trim();
            bool laNhanVien;
            if (nhanVien != null) laNhanVien = true;
            else if (khachHang != null) laNhanVien = false;
            else
            {
                string? maQuyenKH = await LayMaQuyenKhachHangAsync();
                laNhanVien = maQuyenKH == null || !string.Equals(maQuyen, maQuyenKH, StringComparison.OrdinalIgnoreCase);
            }
            HttpContext.Session.SetString("LoaiTK", laNhanVien ? "NhanVien" : "KhachHang");

            // Vai trò trong khu vực quản trị: quản lý (admin) hay nhân viên - theo quyền của tài khoản
            if (laNhanVien)
            {
                var quyen = await _context.QuyenTruyCaps.AsNoTracking()
                    .FirstOrDefaultAsync(q => q.Maquyen == maQuyen);
                bool laQuanLy = PhanQuyen.LaQuyenQuanLy(maQuyen, quyen?.Tenquyen, _configuration);
                HttpContext.Session.SetString(PhanQuyen.KhoaSession, laQuanLy ? PhanQuyen.QuanLy : PhanQuyen.NhanVien);
            }
            else
            {
                HttpContext.Session.Remove(PhanQuyen.KhoaSession);
            }

            if (!laNhanVien)
            {
                // Khách mới chưa có hồ sơ -> phải nhập thông tin trước (nếu không sẽ thiếu MaKH và không thanh toán được)
                if (khachHang == null)
                    return RedirectToAction("NhapThongTinKhachHang", "KhachHang");

                TempData["LoginSuccess"] = true;
                return RedirectToAction("DangNhap");
            }

            // Nhân viên / quản trị -> trang tổng quan có lối tắt tới mọi chức năng
            return RedirectToAction("HienThiAdmin", "Admin");
        }

        // XÁC THỰC MÃ OTP
        public IActionResult RequestOtp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestOtp(TaiKhoan taiKhoan)
        {
            if (string.IsNullOrEmpty(taiKhoan.Email))
            {
                TempData["ErrorMessage"] = "Email không được để trống.";
                return View();
            }

            var tk = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.Email != null && t.Email.ToLower() == taiKhoan.Email.ToLower());

            if (tk == null)
            {
                TempData["ErrorMessage"] = "Email không tồn tại.";
                return View();
            }

            int randomOtp = RandomNumberGenerator.GetInt32(100000, 1000000);
            string otp = randomOtp.ToString();
            tk.Otp = randomOtp;
            await _context.SaveChangesAsync();

            _taiKhoanRepo.SendOtpEmail(taiKhoan.Email, otp);

            TempData["SuccessMessageOTP"] = "Mã OTP đã được gửi đến email của bạn.";
            return RedirectToAction("ResetPassword");
        }

        // THAY ĐỔI MẬT KHẨU
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(TaiKhoan model)
        {
            if (string.IsNullOrEmpty(model.Tendangnhap) || string.IsNullOrEmpty(model.Matkhau) ||
                string.IsNullOrEmpty(model.XacNhanMatKhau) || model.Otp == null || model.Otp == 0)
            {
                TempData["ErrorMessage"] = "Thông tin đăng nhập không chính xác.";
                return View(model);
            }

            if (model.Matkhau.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 6 ký tự.";
                return View(model);
            }

            var tk = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.Tendangnhap == model.Tendangnhap && t.Otp == model.Otp);

            if (tk != null)
            {
                if (model.Matkhau == model.XacNhanMatKhau)
                {
                    tk.Matkhau = model.Matkhau;
                    tk.Otp = null;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("ResetPassword");
                }
                else
                {
                    ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                }
            }
            else
            {
                TempData["ErrorMessageOTP"] = "Tên đăng nhập hoặc mã OTP không chính xác.";
            }

            return View(model);
        }

        // TRANG CHỦ
        public async Task<IActionResult> Welcome(int id = 0, int cart = -1)
        {
            var count = TempData["Cart"];
            ViewBag.CartCount = count;
            if (cart == 0 && id == -1)
            {
                ViewBag.CartCount = 0;
            }

            // Đánh giá mới nhất hiển thị ở trang chủ (lỗi CSDL thì bỏ qua, không làm hỏng trang chủ)
            try
            {
                var dsDanhGia = await _context.DanhGia.AsNoTracking()
                    .Include(d => d.MamonanNavigation)
                    .Include(d => d.ManvNavigation)
                    .Where(d => d.Danhgia != null && d.Danhgia != "")
                    .OrderByDescending(d => d.Ngaydanhgia)
                    .ThenByDescending(d => d.Madanhgia)
                    .Take(6)
                    .ToListAsync();

                var maTKs = dsDanhGia.Select(d => d.Mataikhoan).Where(m => m != null).Distinct().ToList();
                var khach = await _context.KhachHangs.AsNoTracking()
                    .Where(k => maTKs.Contains(k.Mataikhoan)).ToListAsync();
                var tk = await _context.TaiKhoans.AsNoTracking()
                    .Where(t => maTKs.Contains(t.Mataikhoan)).ToListAsync();

                ViewBag.DanhGiaMoi = dsDanhGia;
                ViewBag.TenNguoiDanhGia = dsDanhGia
                    .Where(d => !string.IsNullOrWhiteSpace(d.Mataikhoan))
                    .Select(d => d.Mataikhoan!.Trim()).Distinct()
                    .ToDictionary(m => m, m =>
                        khach.FirstOrDefault(k => k.Mataikhoan?.Trim() == m)?.Hoten
                        ?? tk.FirstOrDefault(t => t.Mataikhoan.Trim() == m)?.Tendangnhap
                        ?? "Thành viên");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không tải được đánh giá cho trang chủ: " + ex.Message);
            }

            return View();
        }
    }
}

