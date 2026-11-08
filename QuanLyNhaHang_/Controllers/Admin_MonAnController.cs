using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;
using System.Text.RegularExpressions;

namespace QuanLyNhaHang_.Controllers
{
    // Món ăn: nhân viên CHỈ XEM (tra cứu món, giá để gọi món cho khách ở Phiếu gọi món);
    // quản lý (admin) mới được thêm / sửa / xóa - các action có [ChiQuanLy].
    // Mọi POST phải có mã antiforgery (form trên giao diện)
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class Admin_MonAnController : Controller
    {
        private readonly DbQlNhaHangContext _context;
        private readonly IWebHostEnvironment _env;

        // Các đuôi ảnh cho phép và dung lượng tối đa (5 MB)
        private static readonly string[] _allowedExt = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        public Admin_MonAnController(DbQlNhaHangContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Lưu file ảnh vào wwwroot/Images/MonAn, trả về tên file đã lưu (null nếu không có file / file không hợp lệ)
        private async Task<string?> LuuAnhAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0 || file.Length > MaxFileSize)
                return null;

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExt.Contains(ext))
                return null;

            // Làm sạch tên file: bỏ ký tự lạ, giữ chữ, số, dấu - _
            string name = Path.GetFileNameWithoutExtension(file.FileName);
            name = Regex.Replace(name, @"[^a-zA-Z0-9_\-]", "_");
            if (string.IsNullOrWhiteSpace(name)) name = "monan";

            string folder = Path.Combine(_env.WebRootPath, "Images", "MonAn");
            Directory.CreateDirectory(folder);

            string fileName = name + ext;
            // Nếu trùng tên thì thêm hậu tố thời gian để không ghi đè ảnh cũ
            if (System.IO.File.Exists(Path.Combine(folder, fileName)))
                fileName = $"{name}_{DateTime.Now:yyyyMMddHHmmss}{ext}";

            using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return fileName;
        }

        // GET: Admin_MonAn
        public IActionResult Index()
        {
            return RedirectToAction("DanhSachMonAn");
        }

        // 1. Hiển thị danh sách món ăn + tìm kiếm theo MÃ hoặc TÊN (không phân biệt dấu)
        public async Task<IActionResult> DanhSachMonAn(string? TenMonCanTim = null)
        {
            var tatCa = await _context.MonAns.AsNoTracking().ToListAsync();
            var listMA = TimKiemMonAn.Loc(tatCa.OrderBy(m => m.Mamonan), TenMonCanTim);

            bool laQuanLy = HttpContext.LaQuanLy();
            ViewBag.LaQuanLy = laQuanLy;
            if (laQuanLy)
            {
                ViewBag.MaMoi = await SinhMaMonAnAsync();
                ViewBag.DsLoaiMon = DanhSachLoaiMon(tatCa);
            }
            return View(listMA);
        }

        // Loại món cố định + các loại đang có trong CSDL (để món cũ vẫn chọn đúng loại)
        private static List<string> DanhSachLoaiMon(IEnumerable<MonAn> dsMon) =>
            TimKiemMonAn.LoaiMonMacDinh
                .Concat(dsMon.Select(m => m.Loaimon?.Trim() ?? "").Where(l => l != ""))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        // Sinh mã món tiếp theo: MA0001, MA0002, ...
        private Task<string> SinhMaMonAnAsync()
            => SinhMaHelper.TaoMaTiepTheoAsync(_context.MonAns.Select(m => m.Mamonan), "MA");

        // GET: Admin_MonAn/MaMoi -> trả mã món kế tiếp (dùng cho nút "Làm mới")
        [ChiQuanLy]
        [HttpGet]
        public async Task<JsonResult> MaMoi() => Json(await SinhMaMonAnAsync());

        // 2. Xử lý Thêm / Sửa / Xóa tập trung qua Request.Form (đã bao gồm Mô tả) - chỉ quản lý
        [ChiQuanLy]
        [HttpPost]
        public async Task<IActionResult> XuLyForm()
        {
            string? maMonAn = Request.Form["MaMonAn"];
            string? action = Request.Form["action"];

            // Thêm mới: mã do hệ thống tự sinh, không lấy từ form
            if (action == "Them")
                maMonAn = await SinhMaMonAnAsync();
            string? tenMon = Request.Form["TenMon"];
            string? loaiMon = Request.Form["LoaiMon"];

            float.TryParse(Request.Form["Gia"], out float gia);
            string? anhMon = Request.Form["TenAnh"];
            string? moTa = Request.Form["MoTa"]; // Lấy thêm Mô tả từ form

            tenMon = tenMon?.Trim();
            loaiMon = loaiMon?.Trim();
            if (action != "Xoa")
            {
                if (string.IsNullOrWhiteSpace(tenMon))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập tên món ăn.";
                    return RedirectToAction("DanhSachMonAn");
                }
                if (tenMon.Length > 20)
                {
                    TempData["ErrorMessage"] = "Tên món tối đa 20 ký tự.";
                    return RedirectToAction("DanhSachMonAn");
                }
                if (string.IsNullOrWhiteSpace(loaiMon))
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn loại món ăn.";
                    return RedirectToAction("DanhSachMonAn");
                }
            }

            if (string.IsNullOrEmpty(maMonAn))
            {
                return RedirectToAction("DanhSachMonAn");
            }

            string maTrim = maMonAn.Trim();

            // Nếu người dùng chọn file ảnh mới thì lưu file và dùng tên file đã lưu
            var fileAnh = Request.Form.Files["AnhMon"];
            string? tenAnhDaLuu = await LuuAnhAsync(fileAnh);
            if (tenAnhDaLuu != null)
            {
                anhMon = tenAnhDaLuu;
            }

            switch (action)
            {
                case "Them":
                    var monAnMoi = new MonAn
                    {
                        Mamonan = maTrim,
                        Tenmon = tenMon,
                        Loaimon = loaiMon,
                        Gia = gia,
                        AnhMon = anhMon,
                        MoTa = moTa // Gán mô tả khi thêm
                    };
                    _context.MonAns.Add(monAnMoi);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã thêm món {tenMon} với mã {maTrim}.";
                    break;

                case "Sua":
                    var monAnSua = await _context.MonAns.FindAsync(maTrim);
                    if (monAnSua != null)
                    {
                        monAnSua.Tenmon = tenMon;
                        monAnSua.Loaimon = loaiMon;
                        monAnSua.Gia = gia;
                        monAnSua.MoTa = moTa; // Cập nhật mô tả khi sửa

                        if (!string.IsNullOrEmpty(anhMon))
                        {
                            monAnSua.AnhMon = anhMon;
                        }

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"Đã cập nhật món {maTrim}.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy món cần sửa. Hãy bấm Edit ở bảng bên dưới trước.";
                    }
                    break;

                case "Xoa":
                    var monAnXoa = await _context.MonAns.FindAsync(maTrim);
                    if (monAnXoa != null)
                    {
                        try
                        {
                            _context.MonAns.Remove(monAnXoa);
                            await _context.SaveChangesAsync();
                            TempData["SuccessMessage"] = $"Đã xóa món {maTrim}.";
                        }
                        catch (DbUpdateException)
                        {
                            TempData["ErrorMessage"] = "Không xóa được: món này đã có trong hóa đơn.";
                        }
                    }
                    break;
            }

            return RedirectToAction("DanhSachMonAn");
        }
    }
}