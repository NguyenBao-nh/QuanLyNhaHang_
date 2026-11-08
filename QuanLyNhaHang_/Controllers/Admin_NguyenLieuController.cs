using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    // Chỉ nhân viên đã đăng nhập; mọi POST phải có mã antiforgery (form trên giao diện)
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class Admin_NguyenLieuController : Controller
    {
        private readonly XuLyNguyenLieu _xuLyNguyenLieu;
        private readonly IConfiguration _cfg;
        private readonly DbQlNhaHangContext _context;

        // Sử dụng Dependency Injection để lấy service đã cấu hình trong Program.cs
        public Admin_NguyenLieuController(XuLyNguyenLieu xuLyNguyenLieu, IConfiguration cfg, DbQlNhaHangContext context)
        {
            _xuLyNguyenLieu = xuLyNguyenLieu;
            _cfg = cfg;
            _context = context;
        }

        // GET: Admin_NguyenLieu/HienThiNguyenLieu
        public async Task<IActionResult> HienThiNguyenLieu()
        {
            // Gọi phương thức từ service nghiệp vụ
            var danhSachNguyenLieu = await _xuLyNguyenLieu.LayThongTinNguyenLieuAsync();
            ViewBag.MucToiThieu = XuLyNguyenLieu.MucToiThieu(_cfg);

            // Nguyên liệu đã có trong phiếu nhập / xuất kho thì không xóa được (giữ lịch sử kho)
            var daNhap = await _context.ChiTietNhapKhos.AsNoTracking().Select(c => c.Manguyenlieu).Distinct().ToListAsync();
            var daXuat = await _context.ChiTietXuatKhos.AsNoTracking().Select(c => c.Manguyenlieu).Distinct().ToListAsync();
            ViewBag.CoLichSu = daNhap.Concat(daXuat).Select(m => m.Trim()).ToHashSet();
            ViewBag.MaMoi = await SinhMaAsync();

            return View(danhSachNguyenLieu);
        }

        // POST: thêm nguyên liệu mới (vd món mới cần nguyên liệu chưa có). Tồn kho ban đầu = 0, nhập hàng ở "Nhập kho".
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemNguyenLieu(string? TenNguyenLieu, string? Dvt, int? DonGia)
        {
            var ten = TenNguyenLieu?.Trim() ?? "";
            var dvt = Dvt?.Trim() ?? "";

            string? loi = null;
            if (ten == "") loi = "Vui lòng nhập tên nguyên liệu.";
            else if (ten.Length > 30) loi = "Tên nguyên liệu tối đa 30 ký tự.";
            else if (dvt == "") loi = "Vui lòng nhập đơn vị tính (kg, lít, bó, xấp...).";
            else if (dvt.Length > 10) loi = "Đơn vị tính tối đa 10 ký tự.";
            else if (DonGia == null || DonGia < 0) loi = "Đơn giá phải là số từ 0 trở lên.";
            else
            {
                var tenThuong = ten.ToLower();
                if (await _context.NguyenLieus.AnyAsync(n => n.Tennguyenlieu != null && n.Tennguyenlieu.ToLower() == tenThuong))
                    loi = $"Nguyên liệu \"{ten}\" đã có trong danh sách.";
            }

            if (loi != null)
            {
                TempData["LoiNL"] = loi;
                TempData["MoFormThem"] = "1";
                return RedirectToAction(nameof(HienThiNguyenLieu));
            }

            // Nguyên liệu thuộc kho đầu tiên (nếu có)
            var maKho = await _context.Khos.AsNoTracking().OrderBy(k => k.Makho).Select(k => k.Makho).FirstOrDefaultAsync();

            var nl = new NguyenLieu
            {
                Manguyenlieu = await SinhMaAsync(),
                Makho = maKho,
                Tennguyenlieu = ten,
                Dvt = dvt,
                Dongia = DonGia
            };
            _context.NguyenLieus.Add(nl);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                TempData["LoiNL"] = "Không lưu được nguyên liệu: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction(nameof(HienThiNguyenLieu));
            }

            TempData["ThanhCongNL"] = $"Đã thêm nguyên liệu {nl.Manguyenlieu} - {ten}. Bấm \"Nhập hàng\" để nhập số lượng vào kho.";
            return RedirectToAction(nameof(HienThiNguyenLieu));
        }

        // POST: xóa nguyên liệu. Chỉ xóa được khi chưa có trong phiếu nhập / xuất kho (khóa ngoại + giữ lịch sử).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaNguyenLieu(string ma)
        {
            ma = ma?.Trim() ?? "";
            var nl = ma == "" ? null : await _context.NguyenLieus.FirstOrDefaultAsync(n => n.Manguyenlieu == ma);
            if (nl == null)
            {
                TempData["LoiNL"] = "Không tìm thấy nguyên liệu (có thể đã bị xóa).";
                return RedirectToAction(nameof(HienThiNguyenLieu));
            }

            bool coNhap = await _context.ChiTietNhapKhos.AnyAsync(c => c.Manguyenlieu == ma);
            bool coXuat = await _context.ChiTietXuatKhos.AnyAsync(c => c.Manguyenlieu == ma);
            if (coNhap || coXuat)
            {
                TempData["LoiNL"] = $"Không xóa được \"{nl.Tennguyenlieu}\": nguyên liệu đã có trong phiếu "
                    + (coNhap && coXuat ? "nhập và xuất kho" : coNhap ? "nhập kho" : "xuất kho")
                    + " (cần giữ để tra cứu lịch sử kho).";
                return RedirectToAction(nameof(HienThiNguyenLieu));
            }

            _context.NguyenLieus.Remove(nl);
            try
            {
                await _context.SaveChangesAsync();
                TempData["ThanhCongNL"] = $"Đã xóa nguyên liệu {ma} - {nl.Tennguyenlieu}.";
            }
            catch (DbUpdateException ex)
            {
                TempData["LoiNL"] = "Không xóa được nguyên liệu: " + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToAction(nameof(HienThiNguyenLieu));
        }

        private Task<string> SinhMaAsync()
            => SinhMaHelper.TaoMaTiepTheoAsync(_context.NguyenLieus.Select(n => n.Manguyenlieu), "NL");
    }
}
