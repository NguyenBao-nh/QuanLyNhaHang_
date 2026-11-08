using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QuanLyNhaHang_.Models;

namespace QuanLyNhaHang_.Services
{
    /// <summary>
    /// Phân quyền trong khu vực quản trị (dùng chung giao diện):
    ///  - QuanLy (admin): CHỈ có Tổng quan (số khách hôm nay), Doanh thu tháng + so sánh tháng,
    ///    thêm / sửa / xóa nhân viên, thêm / sửa / xóa mã giảm giá, Đánh giá.
    ///  - NhanVien: mọi chức năng nghiệp vụ như ban đầu, KHÔNG có Đánh giá, không thấy doanh thu tháng
    ///    (Tổng quan và Quỹ vẫn hiện doanh thu theo ngày), mã giảm giá chỉ được xem.
    /// Vai trò được tính lúc đăng nhập và lưu ở Session "VaiTro".
    /// </summary>
    public static class PhanQuyen
    {
        public const string KhoaSession = "VaiTro";
        public const string QuanLy = "QuanLy";
        public const string NhanVien = "NhanVien";

        // Các controller (trong khu quản trị) tài khoản quản lý được vào
        private static readonly HashSet<string> ControllerCuaAdmin = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin",            // Tổng quan
            "Admin_DoanhThu",   // doanh thu theo tháng, so sánh tháng
            "Admin_NhanVien",   // thêm / sửa / xóa nhân viên
            "DanhGia",          // quản lý đánh giá (các action [KhuVucNhanVien] + [ChiQuanLy])
            "Admin_GiamGia",    // mã giảm giá: quản lý thêm / sửa / xóa, nhân viên chỉ xem
            "Admin_MonAn",      // món ăn: quản lý thêm / sửa / xóa, nhân viên chỉ xem
        };

        public static bool AdminDuocVao(string? controller) => controller != null && ControllerCuaAdmin.Contains(controller);

        public static bool LaQuanLy(this HttpContext http)
            => http.Session.GetString("LoaiTK") == KhuVucNhanVienAttribute.NhanVien
            && http.Session.GetString(KhoaSession) == QuanLy;

        /// <summary>
        /// Mã quyền có phải quyền quản lý không.
        /// 1. Nếu appsettings có "Quyen": { "QuanLy": "Q002,Q01" } -> chỉ các mã đó.
        /// 2. Không cấu hình -> tên quyền chứa "quản lý" / "quản trị" / "admin" / "manager".
        /// </summary>
        public static bool LaQuyenQuanLy(string? maQuyen, string? tenQuyen, IConfiguration? cfg)
        {
            var ma = (maQuyen ?? "").Trim();
            if (ma.Length == 0) return false;

            var cauHinh = cfg?["Quyen:QuanLy"];
            if (!string.IsNullOrWhiteSpace(cauHinh))
                return cauHinh.Split(',', ';').Select(x => x.Trim())
                              .Any(x => string.Equals(x, ma, StringComparison.OrdinalIgnoreCase));

            var ten = BoDau(tenQuyen);
            return ten.Contains("quan ly") || ten.Contains("quan tri") || ten.Contains("admin") || ten.Contains("manager");
        }

        public static bool LaQuyenKhachHang(string? tenQuyen)
        {
            var ten = BoDau(tenQuyen);
            return ten.Contains("khach") || ten.Contains("customer");
        }

        public static string BoDau(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var d = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (var c in d)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }

    /// <summary>
    /// Chỉ tài khoản QUẢN LÝ (admin). Gắn thêm vào sau [KhuVucNhanVien] cho controller / action chỉ dành cho admin.
    /// Nhân viên vào -> quay về trang Tổng quan kèm thông báo; gọi AJAX -> 403.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ChiQuanLyAttribute : ActionFilterAttribute
    {
        public ChiQuanLyAttribute() { Order = 1; }   // chạy sau [KhuVucNhanVien]

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var tempData = (context.Controller as Controller)?.TempData;
            bool laAjax = http.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                       || http.Request.Headers.Accept.ToString().Contains("application/json");

            if (string.IsNullOrEmpty(http.Session.GetString("MaTaiKhoan")))
            {
                if (laAjax) { context.Result = new UnauthorizedResult(); return; }
                context.Result = new RedirectToActionResult("DangNhap", "Account", null);
                return;
            }

            if (!http.LaQuanLy())
            {
                if (laAjax) { context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden); return; }
                if (tempData != null) tempData["ErrorMessage"] = "Chức năng này chỉ dành cho tài khoản quản lý (admin).";
                context.Result = http.Session.GetString("LoaiTK") == KhuVucNhanVienAttribute.NhanVien
                    ? new RedirectToActionResult("HienThiAdmin", "Admin", null)
                    : new RedirectToActionResult("Welcome", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
