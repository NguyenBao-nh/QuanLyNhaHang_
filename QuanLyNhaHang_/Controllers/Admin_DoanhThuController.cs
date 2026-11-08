using Microsoft.AspNetCore.Mvc;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    // Trang Doanh thu: CHỈ tài khoản QUẢN LÝ (admin).
    // Doanh thu theo tháng (12 tháng trong năm, từng ngày của tháng đang xem) và so sánh 2 tháng bất kỳ.
    [KhuVucNhanVien]
    [ChiQuanLy]
    public class Admin_DoanhThuController : Controller
    {
        private readonly XuLyQuy _xuLyQuy;

        public Admin_DoanhThuController(XuLyQuy xuLyQuy)
        {
            _xuLyQuy = xuLyQuy;
        }

        // Admin_DoanhThu/Index?nam=2026&thang=11&thangA=11&namA=2026&thangB=10&namB=2026
        public async Task<IActionResult> Index(int? nam = null, int? thang = null,
                                               int? thangA = null, int? namA = null, int? thangB = null, int? namB = null)
        {
            var homNay = DateTime.Today;
            int n = nam is >= 2000 and <= 2100 ? nam.Value : homNay.Year;
            int t = thang is >= 1 and <= 12 ? thang.Value : (n == homNay.Year ? homNay.Month : 12);

            var model = new ThongKeDoanhThu { Nam = n, Thang = t };

            // 12 tháng trong năm
            var dauNam = new DateTime(n, 1, 1);
            var dl = await _xuLyQuy.DuLieuAsync(dauNam, dauNam.AddYears(1));
            if (dl.Loi != null) TempData["LoiDoanhThu"] = dl.Loi;
            model.TheoThang = XuLyQuy.TheoThang(dl, n);

            // Từng ngày của tháng đang xem (đến hôm nay)
            var dauThang = new DateTime(n, t, 1);
            model.TheoNgayTrongThang = Enumerable.Range(0, DateTime.DaysInMonth(n, t))
                .Select(i => dauThang.AddDays(i))
                .Where(x => x <= homNay)
                .Select(x =>
                {
                    var dong = new DongNgayQuy { Ngay = x };
                    dl.DienVao(dong, x, x.AddDays(1));
                    return dong;
                }).ToList();

            // So sánh 2 tháng (mặc định: tháng đang xem với tháng trước đó)
            var thangTruoc = dauThang.AddMonths(-1);
            int tA = thangA is >= 1 and <= 12 ? thangA.Value : t;
            int nA = namA is >= 2000 and <= 2100 ? namA.Value : n;
            int tB = thangB is >= 1 and <= 12 ? thangB.Value : thangTruoc.Month;
            int nB = namB is >= 2000 and <= 2100 ? namB.Value : thangTruoc.Year;
            var batDauA = new DateTime(nA, tA, 1);
            var batDauB = new DateTime(nB, tB, 1);
            model.SoSanh = new SoSanhThangQuy
            {
                ThangA = tA,
                NamA = nA,
                ThangB = tB,
                NamB = nB,
                A = await _xuLyQuy.TongHopAsync(batDauA, batDauA.AddMonths(1)),
                B = await _xuLyQuy.TongHopAsync(batDauB, batDauB.AddMonths(1)),
            };

            return View(model);
        }
    }
}
