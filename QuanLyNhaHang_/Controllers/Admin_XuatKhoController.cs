using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    // Chỉ nhân viên đã đăng nhập; mọi POST phải có mã antiforgery (form trên giao diện)
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class Admin_XuatKhoController : Controller
    {
        private readonly DbQlNhaHangContext _context;
        private readonly XuLyXuatKho _xuLyXuatKho;
        private readonly XuLyNguyenLieu _xuLyNguyenLieu;

        public Admin_XuatKhoController(DbQlNhaHangContext context, XuLyXuatKho xuLyXuatKho, XuLyNguyenLieu xuLyNguyenLieu)
        {
            _context = context;
            _xuLyXuatKho = xuLyXuatKho;
            _xuLyNguyenLieu = xuLyNguyenLieu;
        }

        // GET: Admin_XuatKho  (không có view Index -> chuyển sang trang danh sách)
        public IActionResult Index()
        {
            return RedirectToAction(nameof(HienThiXuatKho));
        }

        // Danh sách phiếu xuất kho
        public async Task<IActionResult> HienThiXuatKho()
        {
            var ds = await _xuLyXuatKho.LayThongTinXuatKhoAsync();
            return View(ds);
        }

        // Dựng dữ liệu hiển thị cho form thêm phiếu
        [NonAction]
        private async Task<ThemPhieuXuatKhoViewModel> TaoViewModelAsync(ThemPhieuXuatKhoInput? input = null)
        {
            var maNvSession = HttpContext.Session.GetString("MaNV")?.Trim();
            if (string.IsNullOrEmpty(maNvSession)) maNvSession = null;

            var vm = new ThemPhieuXuatKhoViewModel
            {
                MaXuatKho = await _xuLyXuatKho.SinhMaPhieuXuatKhoAsync(),
                MaNVSession = maNvSession,
                MaNV = maNvSession ?? input?.MaNV?.Trim(),
                NgayXuatKho = input?.NgayXuatKho ?? DateTime.Now,
                ChiTiet = input?.ChiTiet ?? new List<ChiTietXuatKhoInput>()
            };

            var nguyenLieus = await _xuLyNguyenLieu.LayThongTinNguyenLieuAsync();
            vm.NguyenLieus = nguyenLieus
                .Select(n => new NguyenLieuXuatOption
                {
                    Ma = n.Manguyenlieu.Trim(),
                    Ten = n.Tennguyenlieu ?? "",
                    Dvt = n.Dvt ?? "",
                    DonGia = n.Dongia ?? 0,
                    Ton = n.Soluongton ?? 0
                })
                .ToList();

            // Chỉ cần danh sách nhân viên khi session không có MaNV
            if (maNvSession == null)
            {
                vm.NhanViens = await _context.NhanViens
                    .AsNoTracking()
                    .OrderBy(n => n.Manv)
                    .Select(n => new NhanVienXuatOption
                    {
                        Ma = n.Manv.Trim(),
                        HoTen = n.Hoten ?? ""
                    })
                    .ToListAsync();
            }

            return vm;
        }

        // GET: Form thêm phiếu xuất kho (phiếu + chi tiết trên cùng 1 trang)
        [HttpGet]
        public async Task<IActionResult> ThemPhieuXuatKho()
        {
            var vm = await TaoViewModelAsync();
            return View("ThemPhieuXuatKho", vm);
        }

        // POST: Lưu phiếu + chi tiết trong MỘT lần
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemPhieuXuatKho(ThemPhieuXuatKhoInput input)
        {
            var maNvSession = HttpContext.Session.GetString("MaNV")?.Trim();
            var kq = await _xuLyXuatKho.TaoPhieuXuatKhoAsync(maNvSession, input);

            if (!kq.ThanhCong)
            {
                foreach (var loi in kq.Loi) ModelState.AddModelError("", loi);
                var vmLoi = await TaoViewModelAsync(input);
                return View("ThemPhieuXuatKho", vmLoi);
            }

            TempData["ThanhCong"] = $"Đã tạo phiếu xuất kho {kq.MaPhieu} (tổng tiền {kq.TongTien:N0} đ).";
            return RedirectToAction(nameof(HienThiXuatKho));
        }

        // Xem chi tiết phiếu xuất kho
        public async Task<IActionResult> chitietXuatKho(string maXuatKho)
        {
            var chiTietXuatKho = await _xuLyXuatKho.GetChiTietXuatKhoAsync(maXuatKho);
            ViewBag.MaXuatKho = (maXuatKho ?? string.Empty).Trim();
            return View(chiTietXuatKho);
        }
    }
}
