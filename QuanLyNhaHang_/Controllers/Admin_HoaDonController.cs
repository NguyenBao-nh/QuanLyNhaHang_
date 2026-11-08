using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class Admin_HoaDonController : Controller
    {
        private readonly DbQlNhaHangContext _context;

        private readonly XuLyKet _xuLyKet;

        public Admin_HoaDonController(DbQlNhaHangContext context, XuLyKet xuLyKet)
        {
            _context = context;
            _xuLyKet = xuLyKet;
        }

        // 1. Danh sách phiếu nhập kho
        public async Task<IActionResult> HienThiHoaDon()
        {
            var listHD = await _context.PhieuNhapKhos
                .AsNoTracking()
                .Include(p => p.ManvNavigation)   // tên nhân viên lập phiếu
                .OrderByDescending(p => p.Ngaynk)
                .ToListAsync();
            return View("HienThiHoaDon", listHD);
        }

        // 2. Sinh mã phiếu nhập: PNK0001, PNK0002, ... (lấy số lớn nhất hiện có + 1)
        [NonAction]
        private async Task<string> GeneratePhieuNhapKhoAsync()
        {
            const string prefix = "PNK";

            var cacMa = await _context.PhieuNhapKhos
                .AsNoTracking()
                .Select(p => p.Manhapkho)
                .ToListAsync();

            int max = 0;
            foreach (var ma in cacMa)
            {
                var m = ma?.Trim();
                if (!string.IsNullOrEmpty(m) &&
                    m.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(m.Substring(prefix.Length), out int so) &&
                    so > max)
                {
                    max = so;
                }
            }
            return prefix + (max + 1).ToString("D4");
        }
        [NonAction]
        private async Task<ThemPhieuNhapKhoViewModel> TaoViewModelAsync(ThemPhieuNhapKhoInput? input = null)
        {
            var maNvSession = HttpContext.Session.GetString("MaNV")?.Trim();
            if (string.IsNullOrEmpty(maNvSession)) maNvSession = null;

            var vm = new ThemPhieuNhapKhoViewModel
            {
                MaNhapKho = await GeneratePhieuNhapKhoAsync(),
                MaNVSession = maNvSession,
                MaNV = maNvSession ?? input?.MaNV?.Trim(),
                NgayNhapKho = input?.NgayNhapKho ?? DateTime.Now,
                ChiTiet = input?.ChiTiet ?? new List<ChiTietNhapKhoInput>(),
                TraTienMat = input?.TraTienMat ?? false,
                TienKet = await _xuLyKet.TienKetHienTaiAsync()
            };

            vm.NguyenLieus = await _context.NguyenLieus
                .AsNoTracking()
                .OrderBy(n => n.Tennguyenlieu)
                .Select(n => new NguyenLieuOption
                {
                    Ma = n.Manguyenlieu.Trim(),
                    Ten = n.Tennguyenlieu ?? "",
                    Dvt = n.Dvt ?? "",
                    DonGia = n.Dongia ?? 0
                })
                .ToListAsync();


            if (maNvSession == null)
            {
                vm.NhanViens = await _context.NhanViens
                    .AsNoTracking()
                    .OrderBy(n => n.Manv)
                    .Select(n => new NhanVienOption
                    {
                        Ma = n.Manv.Trim(),
                        HoTen = n.Hoten ?? ""
                    })
                    .ToListAsync();
            }

            return vm;
        }

        [HttpGet]
        public async Task<IActionResult> ThemHoaDon()
        {
            var vm = await TaoViewModelAsync();
            return View("ThemHoaDon", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemHoaDon(ThemPhieuNhapKhoInput input)
        {
            // 4.1 Xác định nhân viên: ưu tiên Session, nếu không có thì lấy từ ô chọn trên form
            var maNv = (HttpContext.Session.GetString("MaNV") ?? input.MaNV)?.Trim();
            if (string.IsNullOrEmpty(maNv) ||
                !await _context.NhanViens.AnyAsync(n => n.Manv == maNv))
            {
                ModelState.AddModelError("", "Vui lòng chọn nhân viên lập phiếu hợp lệ.");
            }

            // 4.2 Gộp các dòng trùng nguyên liệu, bỏ dòng trống / số lượng <= 0
            var dong = (input.ChiTiet ?? new List<ChiTietNhapKhoInput>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Manguyenlieu) && x.Soluong.GetValueOrDefault() > 0)
                .GroupBy(x => x.Manguyenlieu!.Trim())
                .Select(g => new { Ma = g.Key, SoLuong = g.Sum(x => (long)x.Soluong!.Value) })
                .ToList();

            if (dong.Count == 0)
            {
                ModelState.AddModelError("", "Phiếu nhập kho phải có ít nhất một nguyên liệu với số lượng lớn hơn 0.");
            }

            // 4.3 Kiểm tra mã nguyên liệu + lấy đơn giá từ DB (không tin dữ liệu client gửi lên)
            var chiTietMoi = new List<ChiTietNhapKho>();
            long tongTien = 0;

            if (ModelState.IsValid)
            {
                var dsMa = dong.Select(d => d.Ma).ToList();
                var nguyenLieus = (await _context.NguyenLieus
                        .AsNoTracking()
                        .Where(n => dsMa.Contains(n.Manguyenlieu))
                        .Select(n => new { n.Manguyenlieu, n.Dongia })
                        .ToListAsync())
                    .ToDictionary(n => n.Manguyenlieu.Trim());

                foreach (var d in dong)
                {
                    if (!nguyenLieus.TryGetValue(d.Ma, out var nl))
                    {
                        ModelState.AddModelError("", $"Không tìm thấy nguyên liệu có mã '{d.Ma}'.");
                        continue;
                    }

                    long thanhTien = d.SoLuong * (nl.Dongia ?? 0);
                    tongTien += thanhTien;

                    if (d.SoLuong > int.MaxValue || thanhTien > int.MaxValue || tongTien > int.MaxValue)
                    {
                        ModelState.AddModelError("", "Số lượng hoặc thành tiền quá lớn.");
                        break;
                    }

                    chiTietMoi.Add(new ChiTietNhapKho
                    {
                        Manguyenlieu = nl.Manguyenlieu,
                        Soluong = (int)d.SoLuong,
                        Thanhtien = (int)thanhTien
                    });
                }
            }

            // 4.3b Trả tiền mặt từ két: phải có ca đang mở và két đủ tiền
            CaLamViec? caKet = null;
            if (ModelState.IsValid && input.TraTienMat)
            {
                caKet = await _xuLyKet.LayCaDangMoAsync();
                if (caKet == null)
                    ModelState.AddModelError("", "Chưa mở ca làm việc nên không lấy tiền két được. Mở ca, hoặc bỏ chọn \"Trả bằng tiền mặt từ két\".");
                else
                {
                    int tienKet = (await _xuLyKet.TinhAsync(caKet)).TienKet;
                    if (tongTien > tienKet)
                        ModelState.AddModelError("", $"Tiền trong két chỉ còn {tienKet:N0}đ, không đủ trả {tongTien:N0}đ tiền hàng.");
                }
            }

            // 4.4 Có lỗi -> hiển thị lại form, giữ nguyên các dòng đã nhập
            if (!ModelState.IsValid)
            {
                var vmLoi = await TaoViewModelAsync(input);
                return View("ThemHoaDon", vmLoi);
            }

            // 4.5 Tạo phiếu (mã phiếu luôn do server sinh lại để tránh trùng khi mở nhiều tab / F5)
            var maPhieu = await GeneratePhieuNhapKhoAsync();
            foreach (var ct in chiTietMoi) ct.Manhapkho = maPhieu;

            var phieu = new PhieuNhapKho
            {
                Manhapkho = maPhieu,
                Manv = maNv,
                Ngaynk = input.NgayNhapKho ?? DateTime.Now,
                Tongtien = (int)tongTien,
                ChiTietNhapKhos = chiTietMoi
            };

            try
            {
                _context.PhieuNhapKhos.Add(phieu);
                // Trả tiền hàng bằng tiền mặt -> trừ két (lưu cùng phiếu trong 1 lần SaveChanges)
                if (caKet != null)
                    _xuLyKet.ThemGiaoDich(caKet, LoaiGiaoDichKet.NhapKho, (int)tongTien, "Trả tiền hàng nhập kho", maPhieu, maNv);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Không lưu được phiếu nhập kho: " + (ex.InnerException?.Message ?? ex.Message));
                var vmLoi = await TaoViewModelAsync(input);
                return View("ThemHoaDon", vmLoi);
            }

            TempData["ThanhCong"] = $"Đã tạo phiếu nhập kho {maPhieu} (tổng tiền {tongTien:N0} đ)."
                + (caKet != null ? " Đã trừ tiền mặt trong két." : "");
            return RedirectToAction(nameof(HienThiHoaDon));
        }

        // 5. Xem chi tiết phiếu nhập kho
        public async Task<IActionResult> ChiTietNhapKho(string MaNhapKho)
        {
            var ma = (MaNhapKho ?? string.Empty).Trim();

            // Chiếu thẳng sang ViewModel, không load nguyên entity NguyenLieu
            var model = await _context.ChiTietNhapKhos
                .AsNoTracking()
                .Where(c => c.Manhapkho == ma)
                .Select(c => new ChiTietNhapKhoViewModel
                {
                    MaNhapKho = c.Manhapkho.Trim(),
                    MaNguyenLieu = c.Manguyenlieu.Trim(),
                    TenNguyenLieu = c.ManguyenlieuNavigation.Tennguyenlieu,
                    SoLuong = c.Soluong ?? 0,
                    ThanhTien = c.Thanhtien ?? 0
                })
                .ToListAsync();

            ViewBag.MaNhapKho = ma;
            // Thông tin phiếu + nhân viên lập phiếu
            ViewBag.Phieu = await _context.PhieuNhapKhos.AsNoTracking()
                .Include(p => p.ManvNavigation)
                .FirstOrDefaultAsync(p => p.Manhapkho == ma);
            return View("ChiTietNhapKho", model);
        }
    }
}
