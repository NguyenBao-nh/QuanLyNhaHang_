using Microsoft.AspNetCore.Mvc;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{

    // Trang Quỹ (chức năng của NHÂN VIÊN): tiền trong két (ca đang mở), thu chi tiền mặt (bồi thường, chi tiêu vặt),
    // doanh thu THEO NGÀY,
    // tiền chi tiêu theo tháng (nhập kho + chi vặt) và tiền hàng xuất kho.
    // Không hiện doanh thu theo tháng - phần đó nằm ở trang Doanh thu của tài khoản quản lý.
    [KhuVucNhanVien]
    public class Admin_QuyController : Controller
    {
        private readonly XuLyKet _xuLyKet;
        private readonly XuLyQuy _xuLyQuy;
        private readonly IConfiguration _cfg;

        public Admin_QuyController(XuLyKet xuLyKet, XuLyQuy xuLyQuy, IConfiguration cfg)
        {
            _xuLyKet = xuLyKet;
            _xuLyQuy = xuLyQuy;
            _cfg = cfg;
        }

        // Admin_Quy/Index?ngay=2026-11-08&nam=2026&thang=11
        public async Task<IActionResult> Index(DateTime? ngay = null, int? nam = null, int? thang = null)
        {
            var homNay = DateTime.Today;

            // Chỉ xem hôm nay nếu bật "PhanQuyen": { "NhanVienChiXemHomNay": true }
            bool chiHomNay = _cfg.GetValue<bool?>("PhanQuyen:NhanVienChiXemHomNay") ?? false;
            var d = (ngay ?? homNay).Date;
            if (chiHomNay || d > homNay || d.Year < 2000) d = homNay;

            int n = nam is >= 2000 and <= 2100 ? nam.Value : homNay.Year;
            int t = thang is >= 1 and <= 12 ? thang.Value : (n == homNay.Year ? homNay.Month : 12);

            var model = new ThongKeQuy { Ngay = d, ChiXemHomNay = chiHomNay, Nam = n, Thang = t };

            // Tiền két (ca đang mở)
            try
            {
                model.CaDangMo = await _xuLyKet.LayCaDangMoAsync();
                if (model.CaDangMo != null) model.TienKet = (await _xuLyKet.TinhAsync(model.CaDangMo)).TienKet;
            }
            catch (Exception ex)
            {
                TempData["LoiQuy"] = "Chưa đọc được dữ liệu ca: " + ex.Message;
            }

            // Doanh thu theo ngày
            model.NgayDangXem = await _xuLyQuy.TongHopAsync(d, d.AddDays(1));
            model.HoaDonNgay = await _xuLyQuy.HoaDonTrongNgayAsync(d);

            // Phiếu thu / chi trong ngày (bồi thường, chi tiêu vặt) - cùng dữ liệu với trang Ca làm việc & két
            try { model.ThuChiNgay = await _xuLyQuy.ThuChiTrongNgayAsync(d, model.CaDangMo?.Maca); }
            catch (Exception ex) { TempData["LoiQuy"] = "Chưa đọc được phiếu thu / chi: " + ex.Message; }

            // Chi tiêu theo tháng (view chỉ hiện các cột chi tiêu / xuất kho)
            var dauNam = new DateTime(n, 1, 1);
            var dl = await _xuLyQuy.DuLieuAsync(dauNam, dauNam.AddYears(1));
            if (dl.Loi != null) TempData["LoiQuy"] = dl.Loi;
            model.TheoThang = XuLyQuy.TheoThang(dl, n);

            return View(model);
        }
    }
}
