using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    // Chỉ nhân viên đã đăng nhập; mọi POST phải có mã antiforgery (form trên giao diện)
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class AdminController : Controller
    {
        private readonly DbQlNhaHangContext _context;

        public AdminController(DbQlNhaHangContext context)
        {
            _context = context;
        }

        // Một thống kê lỗi (ví dụ lệch cột CSDL) không được làm sập cả trang tổng quan -> hiển thị "—"
        private static async Task<T?> An<T>(Func<Task<T>> f) where T : struct
        {
            try { return await f(); } catch { return null; }
        }

        // GET: Admin/HienThiAdmin  (trang tổng quan, nội dung khác nhau theo vai trò)
        public async Task<IActionResult> HienThiAdmin()
        {
            var homNay = DateTime.Today;
            var ngayMai = homNay.AddDays(1);
            bool laQuanLy = HttpContext.LaQuanLy();
            ViewBag.LaQuanLy = laQuanLy;

            // Đơn trực tuyến đã hủy hoặc chưa đối soát tiền không tính vào số hóa đơn / doanh thu
            var hdHomNay = _context.HoaDons.Where(h => h.Ngaylap >= homNay && h.Ngaylap < ngayMai
                && h.Trangthai != TrangThaiDon.DaHuy && h.Trangthai != TrangThaiDon.ChoXacNhan);
            ViewBag.HoaDonHomNay = await An(() => hdHomNay.CountAsync());

            if (laQuanLy)
            {
                // Quản lý: số khách hôm nay (đếm hóa đơn), doanh thu tháng này, nhân viên, mã giảm giá, đánh giá
                var dauThang = new DateTime(homNay.Year, homNay.Month, 1);
                ViewBag.DoanhThuThangNay = await An(() => _context.HoaDons
                    .Where(h => h.Ngaylap >= dauThang && h.Ngaylap < ngayMai && h.Trangthai != TrangThaiDon.DaHuy && h.Trangthai != TrangThaiDon.ChoXacNhan)
                    .SumAsync(h => h.Tongtien ?? 0));
                ViewBag.SoNhanVien = await An(() => _context.NhanViens.CountAsync());
                ViewBag.SoMon = await An(() => _context.MonAns.CountAsync());
                ViewBag.SoMaGiamGia = await An(() => _context.MaGiamGia.CountAsync(g => g.NgayKT >= homNay && g.SoLuong > 0));
                ViewBag.SoDanhGia = await An(() => _context.DanhGia.CountAsync());
                ViewBag.DanhGiaChuaTraLoi = await An(() => _context.DanhGia.CountAsync(d => d.Phanhoi == null || d.Phanhoi == ""));
                return View();
            }

            // Nhân viên: như ban đầu, có doanh thu HÔM NAY (doanh thu theo ngày)
            ViewBag.SoMon = await An(() => _context.MonAns.CountAsync());
            ViewBag.BanTrong = await An(() => _context.Bans.CountAsync(b => b.Trangthai == "Trống"));
            ViewBag.TongBan = await An(() => _context.Bans.CountAsync());
            ViewBag.DoanhThuHomNay = await An(() => hdHomNay.SumAsync(h => h.Tongtien ?? 0));
            ViewBag.DonChoXacNhan = await An(() => _context.HoaDons.CountAsync(h => h.Loaidon == LoaiDon.TrucTuyen && h.Trangthai == TrangThaiDon.ChoXacNhan));
            ViewBag.DatBanSapToi = await An(() => _context.PhieuDatBans.CountAsync(p => p.Ngaydat >= homNay && (p.Trangthai == null || p.Trangthai == "" || p.Trangthai == TrangThaiDatBan.ChoXepBan)));
            ViewBag.PhieuDangPhucVu = await An(() => _context.PhieuGoiMons.CountAsync());
            return View();
        }

        // GET: Admin/NhapKho  -> view cũ chỉ là bản nháp tĩnh (form trỏ tới action không tồn tại).
        // Chức năng nhập kho thật nằm ở Admin_HoaDon.
        public IActionResult NhapKho() => RedirectToAction("HienThiHoaDon", "Admin_HoaDon");
    }
}
