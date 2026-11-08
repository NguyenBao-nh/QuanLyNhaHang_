using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    // Quản lý nhân viên: CHỈ tài khoản quản lý (admin). Nhân viên gõ URL cũng bị chặn ở server.
    [KhuVucNhanVien]
    [ChiQuanLy]
    [AutoValidateAntiforgeryToken]
    public class Admin_NhanVienController : Controller
    {
        private readonly DbQlNhaHangContext _context;
        private readonly IConfiguration _cfg;

        public Admin_NhanVienController(DbQlNhaHangContext context, IConfiguration cfg)
        {
            _context = context;
            _cfg = cfg;
        }

        // GET: Admin_NhanVien/Index?q=...
        public async Task<IActionResult> Index(string? q)
        {
            var dsQuyen = await _context.QuyenTruyCaps.AsNoTracking().ToListAsync();
            var ds = await _context.NhanViens.AsNoTracking()
                .Include(n => n.MataikhoanNavigation)
                .OrderBy(n => n.Manv)
                .ToListAsync();

            var tuKhoa = PhanQuyen.BoDau(q);
            var kq = ds.Select(n =>
            {
                var tk = n.MataikhoanNavigation;
                var quyen = dsQuyen.FirstOrDefault(x => x.Maquyen.Trim() == (tk?.Maquyen ?? "").Trim());
                return new DongNhanVien
                {
                    NhanVien = n,
                    Tendangnhap = tk?.Tendangnhap,
                    Email = tk?.Email,
                    Maquyen = tk?.Maquyen?.Trim(),
                    Tenquyen = quyen?.Tenquyen,
                    LaQuanLy = PhanQuyen.LaQuyenQuanLy(tk?.Maquyen, quyen?.Tenquyen, _cfg),
                };
            })
            .Where(x => tuKhoa.Length == 0
                     || PhanQuyen.BoDau(x.NhanVien.Manv).Contains(tuKhoa)
                     || PhanQuyen.BoDau(x.NhanVien.Hoten).Contains(tuKhoa)
                     || PhanQuyen.BoDau(x.NhanVien.Chucvu).Contains(tuKhoa)
                     || PhanQuyen.BoDau(x.NhanVien.Sodt).Contains(tuKhoa)
                     || PhanQuyen.BoDau(x.Tendangnhap).Contains(tuKhoa))
            .ToList();

            ViewBag.Q = q;
            ViewBag.MaNVDangNhap = HttpContext.Session.GetString("MaNV");
            return View(kq);
        }

        // GET: Admin_NhanVien/Them
        public async Task<IActionResult> Them()
        {
            await NapDanhSachQuyenAsync(null);
            return View("ThongTin", new NhanVienForm());
        }

        // GET: Admin_NhanVien/Sua/NV0001
        public async Task<IActionResult> Sua(string id)
        {
            var nv = await _context.NhanViens.AsNoTracking()
                .Include(n => n.MataikhoanNavigation)
                .FirstOrDefaultAsync(n => n.Manv == (id ?? "").Trim());
            if (nv == null)
            {
                TempData["LoiNV"] = "Không tìm thấy nhân viên.";
                return RedirectToAction(nameof(Index));
            }
            var tk = nv.MataikhoanNavigation;
            var f = new NhanVienForm
            {
                Manv = nv.Manv.Trim(),
                Mataikhoan = nv.Mataikhoan.Trim(),
                Hoten = nv.Hoten,
                Chucvu = nv.Chucvu,
                Sodt = nv.Sodt?.Trim(),
                Luong = nv.Luong,
                Sogiolam = nv.Sogiolam,
                Tendangnhap = tk?.Tendangnhap,
                Email = tk?.Email,
                Maquyen = tk?.Maquyen?.Trim(),
            };
            await NapDanhSachQuyenAsync(f.Maquyen);
            return View("ThongTin", f);
        }

        // POST: Admin_NhanVien/Luu  (thêm mới khi Manv rỗng, ngược lại là sửa)
        [HttpPost]
        public async Task<IActionResult> Luu(NhanVienForm f)
        {
            f.Hoten = f.Hoten?.Trim();
            f.Chucvu = string.IsNullOrWhiteSpace(f.Chucvu) ? null : f.Chucvu.Trim();
            f.Sodt = string.IsNullOrWhiteSpace(f.Sodt) ? null : f.Sodt.Trim();
            f.Tendangnhap = f.Tendangnhap?.Trim();
            f.Email = string.IsNullOrWhiteSpace(f.Email) ? null : f.Email.Trim();
            f.Maquyen = f.Maquyen?.Trim();
            if (string.IsNullOrEmpty(f.Matkhau)) f.Matkhau = null;

            NhanVien? nv = null;
            TaiKhoan? tk = null;
            if (!f.LaThemMoi)
            {
                nv = await _context.NhanViens.FirstOrDefaultAsync(n => n.Manv == f.Manv!.Trim());
                if (nv == null)
                {
                    TempData["LoiNV"] = "Không tìm thấy nhân viên cần sửa.";
                    return RedirectToAction(nameof(Index));
                }
                tk = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Mataikhoan == nv.Mataikhoan);
                f.Mataikhoan = nv.Mataikhoan.Trim();
            }

            if (f.LaThemMoi && f.Matkhau == null)
                ModelState.AddModelError(nameof(f.Matkhau), "Nhập mật khẩu cho tài khoản mới.");

            // Quyền phải là quyền nhân viên / quản lý (không phải quyền khách hàng)
            var quyen = await _context.QuyenTruyCaps.AsNoTracking().FirstOrDefaultAsync(q => q.Maquyen == f.Maquyen);
            if (quyen == null || PhanQuyen.LaQuyenKhachHang(quyen.Tenquyen))
                ModelState.AddModelError(nameof(f.Maquyen), "Chọn một quyền nhân viên hoặc quản lý.");

            // Trùng tên đăng nhập / email với tài khoản khác
            string maTkHienTai = tk?.Mataikhoan ?? "";
            if (!string.IsNullOrEmpty(f.Tendangnhap))
            {
                var ten = f.Tendangnhap.ToLower();
                if (await _context.TaiKhoans.AnyAsync(t => t.Tendangnhap!.ToLower() == ten && t.Mataikhoan != maTkHienTai))
                    ModelState.AddModelError(nameof(f.Tendangnhap), "Tên đăng nhập đã được dùng.");
            }
            if (!string.IsNullOrEmpty(f.Email))
            {
                var mail = f.Email.ToLower();
                if (await _context.TaiKhoans.AnyAsync(t => t.Email!.ToLower() == mail && t.Mataikhoan != maTkHienTai))
                    ModelState.AddModelError(nameof(f.Email), "Email đã được dùng cho tài khoản khác.");
            }

            // Không tự hạ quyền của chính mình (tránh khóa mất tài khoản quản lý đang dùng)
            bool laChinhMinh = nv != null && string.Equals(nv.Manv.Trim(), HttpContext.Session.GetString("MaNV"), StringComparison.OrdinalIgnoreCase);
            if (laChinhMinh && quyen != null && !PhanQuyen.LaQuyenQuanLy(quyen.Maquyen, quyen.Tenquyen, _cfg))
                ModelState.AddModelError(nameof(f.Maquyen), "Không thể tự bỏ quyền quản lý của chính tài khoản đang đăng nhập.");

            if (!ModelState.IsValid)
            {
                await NapDanhSachQuyenAsync(f.Maquyen);
                return View("ThongTin", f);
            }

            try
            {
                if (f.LaThemMoi)
                {
                    tk = new TaiKhoan
                    {
                        Mataikhoan = await SinhMaHelper.TaoMaTiepTheoAsync(_context.TaiKhoans.Select(t => t.Mataikhoan), "TK"),
                        Maquyen = f.Maquyen,
                        Tendangnhap = f.Tendangnhap,
                        Matkhau = f.Matkhau,
                        Email = f.Email,
                        Isemailconfirmed = 1,   // quản lý tạo -> dùng được ngay, không cần xác thực email
                    };
                    _context.TaiKhoans.Add(tk);

                    nv = new NhanVien
                    {
                        Manv = await SinhMaHelper.TaoMaTiepTheoAsync(_context.NhanViens.Select(n => n.Manv), "NV"),
                        Mataikhoan = tk.Mataikhoan,
                    };
                    _context.NhanViens.Add(nv);
                }
                else if (tk == null)
                {
                    // Hồ sơ cũ mất tài khoản -> tạo lại tài khoản
                    if (f.Matkhau == null)
                    {
                        ModelState.AddModelError(nameof(f.Matkhau), "Nhân viên này chưa có tài khoản, hãy nhập mật khẩu để tạo.");
                        await NapDanhSachQuyenAsync(f.Maquyen);
                        return View("ThongTin", f);
                    }
                    tk = new TaiKhoan
                    {
                        Mataikhoan = await SinhMaHelper.TaoMaTiepTheoAsync(_context.TaiKhoans.Select(t => t.Mataikhoan), "TK"),
                        Isemailconfirmed = 1,
                    };
                    _context.TaiKhoans.Add(tk);
                    nv!.Mataikhoan = tk.Mataikhoan;
                }

                nv!.Hoten = f.Hoten;
                nv.Chucvu = f.Chucvu;
                nv.Sodt = f.Sodt;
                nv.Luong = f.Luong;
                nv.Sogiolam = f.Sogiolam ?? nv.Sogiolam ?? 0;

                tk!.Maquyen = f.Maquyen;
                tk.Tendangnhap = f.Tendangnhap;
                tk.Email = f.Email;
                if (f.Matkhau != null) tk.Matkhau = f.Matkhau;

                await _context.SaveChangesAsync();

                // Sửa chính mình -> cập nhật tên hiển thị
                if (laChinhMinh) HttpContext.Session.SetString("HoTen", nv.Hoten ?? "");

                TempData["ThongBaoNV"] = f.LaThemMoi
                    ? $"Đã thêm nhân viên {nv.Manv.Trim()} - {nv.Hoten} (tài khoản: {tk.Tendangnhap})."
                    : $"Đã cập nhật nhân viên {nv.Manv.Trim()} - {nv.Hoten}.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Không lưu được vào CSDL: " + (ex.InnerException?.Message ?? ex.Message));
                await NapDanhSachQuyenAsync(f.Maquyen);
                return View("ThongTin", f);
            }
        }

        // POST: Admin_NhanVien/Xoa
        [HttpPost]
        public async Task<IActionResult> Xoa(string id)
        {
            var ma = (id ?? "").Trim();
            if (string.Equals(ma, HttpContext.Session.GetString("MaNV"), StringComparison.OrdinalIgnoreCase))
            {
                TempData["LoiNV"] = "Không thể xóa chính tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            var nv = await _context.NhanViens.FirstOrDefaultAsync(n => n.Manv == ma);
            if (nv == null)
            {
                TempData["LoiNV"] = "Không tìm thấy nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            // Nhân viên đã lập hóa đơn / phiếu kho / được đánh giá -> giữ lại để không mất lịch sử
            int soHD = await _context.HoaDons.CountAsync(h => h.Manv == ma);
            int soNK = await _context.PhieuNhapKhos.CountAsync(p => p.Manv == ma);
            int soXK = await _context.PhieuXuatKhos.CountAsync(p => p.Manv == ma);
            int soDG = await _context.DanhGia.CountAsync(d => d.Manv == ma);
            if (soHD + soNK + soXK + soDG > 0)
            {
                var ly = new List<string>();
                if (soHD > 0) ly.Add($"{soHD} hóa đơn");
                if (soNK > 0) ly.Add($"{soNK} phiếu nhập kho");
                if (soXK > 0) ly.Add($"{soXK} phiếu xuất kho");
                if (soDG > 0) ly.Add($"{soDG} đánh giá");
                TempData["LoiNV"] = $"Không xóa được {ma} - {nv.Hoten}: nhân viên đã có {string.Join(", ", ly)}. " +
                                    "Xóa sẽ làm mất lịch sử. Nếu nhân viên đã nghỉ, hãy bấm Sửa và đổi mật khẩu để khóa đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var maTk = nv.Mataikhoan;
                _context.NhanViens.Remove(nv);
                await _context.SaveChangesAsync();

                // Xóa luôn tài khoản nếu không còn ai dùng
                var tk = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Mataikhoan == maTk);
                if (tk != null
                    && !await _context.NhanViens.AnyAsync(n => n.Mataikhoan == maTk)
                    && !await _context.KhachHangs.AnyAsync(k => k.Mataikhoan == maTk)
                    && !await _context.DanhGia.AnyAsync(d => d.Mataikhoan == maTk))
                {
                    _context.TaiKhoans.Remove(tk);
                    await _context.SaveChangesAsync();
                }
                TempData["ThongBaoNV"] = $"Đã xóa nhân viên {ma} - {nv.Hoten}.";
            }
            catch (DbUpdateException ex)
            {
                TempData["LoiNV"] = "Không xóa được: " + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }

        // Danh sách quyền cho ô chọn: bỏ các quyền khách hàng
        private async Task NapDanhSachQuyenAsync(string? chon)
        {
            var ds = await _context.QuyenTruyCaps.AsNoTracking().OrderBy(q => q.Maquyen).ToListAsync();
            ViewBag.DsQuyen = ds
                .Where(q => !PhanQuyen.LaQuyenKhachHang(q.Tenquyen))
                .Select(q => new SelectListItem
                {
                    Value = q.Maquyen.Trim(),
                    Text = $"{q.Maquyen.Trim()} - {q.Tenquyen}" + (PhanQuyen.LaQuyenQuanLy(q.Maquyen, q.Tenquyen, _cfg) ? "  (quản lý - doanh thu tháng, nhân viên, món ăn, mã giảm giá, đánh giá)" : "  (nhân viên)"),
                    Selected = string.Equals(q.Maquyen.Trim(), chon, StringComparison.OrdinalIgnoreCase),
                })
                .ToList();
        }
    }
}
