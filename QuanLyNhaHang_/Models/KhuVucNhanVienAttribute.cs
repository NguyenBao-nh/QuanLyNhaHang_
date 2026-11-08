using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace QuanLyNhaHang_.Services
{
    /// <summary>
    /// Bảo vệ trang quản trị. Gắn [KhuVucNhanVien] lên controller (hoặc action) admin:
    /// 1. Chưa đăng nhập -> chuyển về trang Đăng nhập.
    /// 2. Đăng nhập bằng tài khoản KHÁCH HÀNG -> không cho vào (về trang chủ), kể cả khi gõ đúng URL.
    /// 3. Nhân viên gõ / dán URL trang admin trực tiếp (không bấm từ menu, nút trên giao diện)
    ///    -> chuyển về trang Tổng quan. Chỉ trang Tổng quan được mở trực tiếp (sau khi đăng nhập).
    ///    Tắt bằng "BaoMat": { "ChanGoUrlTrucTiep": false } trong appsettings.json.
    /// Các thao tác thêm / sửa / xóa đều là POST + mã chống giả mạo (antiforgery) nên gõ URL không thực hiện được.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class KhuVucNhanVienAttribute : ActionFilterAttribute
    {
        public const string NhanVien = "NhanVien";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var req = http.Request;
            bool laAjax = req.Headers["X-Requested-With"] == "XMLHttpRequest"
                       || req.Headers.Accept.ToString().Contains("application/json");
            var tempData = (context.Controller as Controller)?.TempData;

            // 1. Chưa đăng nhập
            if (string.IsNullOrEmpty(http.Session.GetString("MaTaiKhoan")))
            {
                if (laAjax) { context.Result = new UnauthorizedResult(); return; }
                if (tempData != null) tempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản nhân viên để vào trang quản trị.";
                context.Result = new RedirectToActionResult("DangNhap", "Account", null);
                return;
            }

            // 2. Không phải nhân viên
            if (http.Session.GetString("LoaiTK") != NhanVien)
            {
                if (laAjax) { context.Result = new ForbidResultKhongCanAuth(); return; }
                if (tempData != null) tempData["CanhBaoQuyen"] = "Tài khoản của bạn không có quyền truy cập trang quản trị.";
                context.Result = new RedirectToActionResult("Welcome", "Account", null);
                return;
            }

            // 2b. Tài khoản QUẢN LÝ (admin) chỉ dùng: Tổng quan, Doanh thu, Nhân viên, Món ăn, Mã giảm giá, Đánh giá.
            //     Các chức năng nghiệp vụ (bàn, gọi món, kho, ca, quỹ...) là của nhân viên.
            if (http.LaQuanLy() && !PhanQuyen.AdminDuocVao(context.RouteData.Values["controller"]?.ToString()))
            {
                if (laAjax) { context.Result = new ForbidResultKhongCanAuth(); return; }
                if (tempData != null) tempData["ErrorMessage"] = "Tài khoản quản lý chỉ dùng các mục Doanh thu, Nhân viên, Món ăn, Mã giảm giá và Đánh giá. Chức năng này dành cho nhân viên.";
                context.Result = new RedirectToActionResult("HienThiAdmin", "Admin", null);
                return;
            }

            // 3. Gõ URL trực tiếp (chỉ chặn GET mở trang; POST đã có antiforgery)
            if (HttpMethods.IsGet(req.Method) && !laAjax && ChanGoUrl(http) && !LaTrangTongQuan(context))
            {
                var referer = req.Headers.Referer.ToString();
                bool tuTrongTrang = Uri.TryCreate(referer, UriKind.Absolute, out var nguon)
                                    && string.Equals(nguon.Host, req.Host.Host, StringComparison.OrdinalIgnoreCase);
                if (!tuTrongTrang)
                {
                    if (tempData != null) tempData["ErrorMessage"] = "Vui lòng mở chức năng bằng menu hoặc nút trên giao diện, không truy cập bằng cách gõ đường dẫn.";
                    context.Result = new RedirectToActionResult("HienThiAdmin", "Admin", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }

        private static bool ChanGoUrl(HttpContext http)
        {
            var cfg = http.RequestServices.GetService<IConfiguration>();
            return cfg?.GetValue<bool?>("BaoMat:ChanGoUrlTrucTiep") ?? true;
        }

        private static bool LaTrangTongQuan(ActionExecutingContext c)
            => string.Equals(c.RouteData.Values["controller"]?.ToString(), "Admin", StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.RouteData.Values["action"]?.ToString(), "HienThiAdmin", StringComparison.OrdinalIgnoreCase);

        // Trả 403 mà không cần cấu hình Authentication (ForbidResult mặc định đòi có authentication scheme)
        private sealed class ForbidResultKhongCanAuth : StatusCodeResult
        {
            public ForbidResultKhongCanAuth() : base(StatusCodes.Status403Forbidden) { }
        }
    }
}
