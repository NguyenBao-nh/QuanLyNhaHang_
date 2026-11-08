using Microsoft.AspNetCore.Mvc;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    public class MonAnController : Controller
    {
        private readonly XuLyThongTinMonAn _xuLyMonAn;

        public MonAnController(XuLyThongTinMonAn xuLyMonAn)
        {
            _xuLyMonAn = xuLyMonAn;
        }

        // Không có view DanhSachMonAn cho khách -> chuyển sang trang Menu
        public IActionResult DanhSachMonAn() => RedirectToAction(nameof(Menu));

        // GET: MonAn/Menu?q=...  (tìm theo mã hoặc tên món, không phân biệt dấu)
        public async Task<IActionResult> Menu(string? q = null)
        {
            var listMA = await _xuLyMonAn.LayThongTinMonAnAsync();
            ViewBag.TuKhoa = q?.Trim();
            return View(TimKiemMonAn.Loc(listMA, q));
        }

        // GET: MonAn/GoiY?q=ga  -> tối đa 8 món gần giống nhất (dùng cho ô tìm kiếm có gợi ý)
        [HttpGet]
        public async Task<JsonResult> GoiY(string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(Array.Empty<object>());

            var listMA = await _xuLyMonAn.LayThongTinMonAnAsync();
            var kq = TimKiemMonAn.Loc(listMA, q)
                .Take(8)
                .Select(m => new
                {
                    ma = m.Mamonan.Trim(),
                    ten = m.Tenmon,
                    loai = m.Loaimon,
                    gia = m.Gia ?? 0,
                    anh = m.AnhMon
                });
            return Json(kq);
        }
    }
}
