using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    /// <summary>
    /// Mục "Đánh giá" (công khai): ai cũng xem được, chỉ người ĐÃ ĐĂNG NHẬP mới được viết đánh giá.
    /// Đánh giá cho Nhà hàng / Món ăn / Nhân viên, chấm 1-5 sao, kèm ảnh (không bắt buộc).
    /// ShowDanhGia + Delete là trang quản trị.
    /// </summary>
    public class DanhGiaController : Controller
    {
        private readonly DbQlNhaHangContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] _duoiAnh = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long DungLuongToiDa = 5 * 1024 * 1024; // 5 MB

        public DanhGiaController(DbQlNhaHangContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private string? MaTaiKhoan => HttpContext.Session.GetString("MaTaiKhoan")?.Trim();

        // ============================================================ TRANG CÔNG KHAI

        // GET: DanhGia  hoặc  DanhGia/Index?loai=MonAn&ma=MA0005
        // GET: DanhGia  hoặc  DanhGia/Index?loai=MonAn&ma=MA0005
        public async Task<IActionResult> Index(string? loai = null, string? ma = null)
        {
            // Lấy TẤT CẢ đánh giá (kể cả đánh giá cũ chưa có số sao)
            var tatCa = await _context.DanhGia.AsNoTracking()
                .Include(d => d.MamonanNavigation)
                .Include(d => d.ManvNavigation)
                .ToListAsync();

            // Thống kê: số lượng đếm tất cả, trung bình sao chỉ tính dòng có sao
            var coSao = tatCa.Where(d => d.Sosao != null).ToList();

            ViewBag.ThongKe = LoaiDanhGia.TatCa.ToDictionary(
                l => l,
                l =>
                {
                    var ds = tatCa.Where(d => (d.Loaidanhgia?.Trim() ?? LoaiDanhGia.NhaHang) == l).ToList();
                    var dsSao = ds.Where(d => d.Sosao != null).ToList();
                    return (SoLuong: ds.Count, TrungBinh: dsSao.Count == 0 ? 0 : dsSao.Average(d => d.Sosao!.Value));
                });
            ViewBag.TongSo = tatCa.Count;
            ViewBag.TrungBinhChung = coSao.Count == 0 ? 0 : coSao.Average(d => d.Sosao!.Value);
            ViewBag.PhanBoSao = Enumerable.Range(1, 5).ToDictionary(s => s, s => coSao.Count(d => d.Sosao == s));

            // Lọc theo loại / đối tượng
            IEnumerable<DanhGia> loc = tatCa;
            if (!string.IsNullOrWhiteSpace(loai) && LoaiDanhGia.TatCa.Contains(loai))
            {
                loc = loc.Where(d => (d.Loaidanhgia?.Trim() ?? LoaiDanhGia.NhaHang) == loai);
                if (!string.IsNullOrWhiteSpace(ma))
                {
                    var m = ma.Trim();
                    loc = loai == LoaiDanhGia.MonAn
                        ? loc.Where(d => d.Mamonan?.Trim() == m)
                        : loai == LoaiDanhGia.NhanVien ? loc.Where(d => d.Manv?.Trim() == m) : loc;
                }
            }

            // Đánh giá không có ngày thì xếp cuối, còn lại xếp theo ngày rồi theo mã
            var ketQua = loc.OrderByDescending(d => d.Ngaydanhgia ?? DateTime.MinValue)
                            .ThenByDescending(d => d.Madanhgia)
                            .ToList();

            // Tên + ảnh đại diện người đánh giá
            var maTKs = ketQua.Select(d => d.Mataikhoan?.Trim())
                              .Where(x => !string.IsNullOrEmpty(x))
                              .Distinct().ToList();
            var khach = await _context.KhachHangs.AsNoTracking()
                .Where(k => maTKs.Contains(k.Mataikhoan)).ToListAsync();
            var nv = await _context.NhanViens.AsNoTracking()
                .Where(n => maTKs.Contains(n.Mataikhoan)).ToListAsync();
            var tk = await _context.TaiKhoans.AsNoTracking()
                .Where(t => maTKs.Contains(t.Mataikhoan)).ToListAsync();

            ViewBag.NguoiDanhGia = maTKs.ToDictionary(
                m => m!,
                m =>
                {
                    var k = khach.FirstOrDefault(x => x.Mataikhoan?.Trim() == m);
                    var n = nv.FirstOrDefault(x => x.Mataikhoan?.Trim() == m);
                    var t = tk.FirstOrDefault(x => x.Mataikhoan.Trim() == m);
                    string ten = k?.Hoten ?? n?.Hoten ?? t?.Tendangnhap ?? "Thành viên";
                    return (Ten: ten, Avatar: k?.Avatar);
                });

            await NapDanhSachChonAsync();
            ViewBag.Loai = loai;
            ViewBag.Ma = ma?.Trim();
            ViewBag.DaDangNhap = !string.IsNullOrEmpty(MaTaiKhoan);
            ViewBag.MaTaiKhoan = MaTaiKhoan;
            return View(ketQua);
        }

        // POST: DanhGia/Them  (chỉ tài khoản đã đăng nhập)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Them(string? Loaidanhgia, string? Mamonan, string? Manv,
                                              int Sosao, string? Noidung, IFormFile? HinhAnh)
        {
            if (string.IsNullOrEmpty(MaTaiKhoan))
            {
                TempData["ThongBaoLoi"] = "Bạn cần đăng nhập để viết đánh giá.";
                return RedirectToAction("DangNhap", "Account");
            }

            Loaidanhgia = Loaidanhgia?.Trim();
            if (Loaidanhgia == null || !LoaiDanhGia.TatCa.Contains(Loaidanhgia))
                return Loi("Vui lòng chọn đối tượng đánh giá.");

            if (Sosao < 1 || Sosao > 5)
                return Loi("Vui lòng chọn số sao từ 1 đến 5.");

            Noidung = Noidung?.Trim();
            if (string.IsNullOrEmpty(Noidung))
                return Loi("Vui lòng nhập nội dung đánh giá.");
            if (Noidung.Length > 1000)
                return Loi("Nội dung tối đa 1000 ký tự.");

            string? maMon = null, maNV = null;
            if (Loaidanhgia == LoaiDanhGia.MonAn)
            {
                maMon = Mamonan?.Trim();
                if (string.IsNullOrEmpty(maMon) || !await _context.MonAns.AnyAsync(m => m.Mamonan == maMon))
                    return Loi("Vui lòng chọn món ăn cần đánh giá.");
            }
            else if (Loaidanhgia == LoaiDanhGia.NhanVien)
            {
                maNV = Manv?.Trim();
                if (string.IsNullOrEmpty(maNV) || !await _context.NhanViens.AnyAsync(n => n.Manv == maNV))
                    return Loi("Vui lòng chọn nhân viên cần đánh giá.");
            }

            string? tenAnh = null;
            if (HinhAnh != null && HinhAnh.Length > 0)
            {
                tenAnh = await LuuAnhAsync(HinhAnh);
                if (tenAnh == null)
                    return Loi("Ảnh không hợp lệ (chỉ nhận JPG, PNG, WEBP, GIF, tối đa 5 MB).");
            }

            string maMoi;
            try
            {
                maMoi = await SinhHelper();
                _context.DanhGia.Add(new DanhGia
                {
                    Madanhgia = maMoi,
                    Mataikhoan = MaTaiKhoan,
                    Loaidanhgia = Loaidanhgia,
                    Mamonan = maMon,
                    Manv = maNV,
                    Sosao = Sosao,
                    Danhgia = Noidung,
                    Hinhanh = tenAnh,
                    Ngaydanhgia = DateTime.Now
                });
                await _context.SaveChangesAsync();
                TempData["ThongBao"] = "Cảm ơn bạn đã đánh giá! Đánh giá của bạn đã được đăng.";
                TempData["DanhGiaMoi"] = maMoi;
            }
            catch (Exception ex)
            {
                // Lỗi CSDL (thiếu cột mới, sai khóa ngoại...) -> báo rõ, không im lặng
                XoaFileAnh(tenAnh);
                return Loi("Không lưu được đánh giá: " + (ex.InnerException?.Message ?? ex.Message));
            }

            // Về danh sách "Tất cả" để thấy ngay đánh giá vừa gửi (được tô sáng ở đầu danh sách)
            return RedirectToAction(nameof(Index));
        }

        // POST: DanhGia/XoaCuaToi  -> người viết tự xóa đánh giá của mình
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaCuaToi(string id)
        {
            var dg = string.IsNullOrWhiteSpace(id) ? null : await _context.DanhGia.FindAsync(id.Trim());
            if (dg != null && !string.IsNullOrEmpty(MaTaiKhoan) && dg.Mataikhoan?.Trim() == MaTaiKhoan)
            {
                XoaFileAnh(dg.Hinhanh);
                _context.DanhGia.Remove(dg);
                await _context.SaveChangesAsync();
                TempData["ThongBao"] = "Đã xóa đánh giá của bạn.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================================================ QUẢN TRỊ

        // GET: DanhGia/ShowDanhGia  (?loc=chuatraloi: chỉ đánh giá chưa được nhà hàng trả lời)
        [KhuVucNhanVien]
        [ChiQuanLy]   // đánh giá chỉ hiện ở tài khoản quản lý
        public async Task<IActionResult> ShowDanhGia(string? loc = null)
        {
            var tatCa = await _context.DanhGia.AsNoTracking()
                .Include(d => d.MamonanNavigation)
                .Include(d => d.ManvNavigation)
                .Include(d => d.MataikhoanNavigation)
                .OrderByDescending(d => d.Ngaydanhgia)
                .ToListAsync();

            ViewBag.SoChuaTraLoi = tatCa.Count(d => string.IsNullOrWhiteSpace(d.Phanhoi));
            ViewBag.Loc = loc;
            var danhGiaList = loc == "chuatraloi"
                ? tatCa.Where(d => string.IsNullOrWhiteSpace(d.Phanhoi)).ToList()
                : tatCa;

            // Tên nhân viên đã trả lời
            var maNVs = tatCa.Select(d => d.Manvphanhoi?.Trim()).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();
            ViewBag.TenNVPhanHoi = (await _context.NhanViens.AsNoTracking()
                    .Where(n => maNVs.Contains(n.Manv)).ToListAsync())
                .ToDictionary(n => n.Manv.Trim(), n => n.Hoten ?? n.Manv.Trim());

            if (!tatCa.Any())
            {
                TempData["ErrorMessage"] = "Hiện chưa có đánh giá nào.";
            }

            return View(danhGiaList);
        }

        // POST: DanhGia/TraLoi -> admin / nhân viên trả lời (hoặc sửa câu trả lời) đánh giá của khách
        [KhuVucNhanVien]
        [ChiQuanLy]   // đánh giá chỉ hiện ở tài khoản quản lý
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TraLoi(string id, string? PhanHoi, string? loc = null)
        {
            var dg = string.IsNullOrWhiteSpace(id) ? null : await _context.DanhGia.FindAsync(id.Trim());
            PhanHoi = PhanHoi?.Trim();
            if (dg == null)
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá (có thể khách đã xóa).";
            else if (string.IsNullOrEmpty(PhanHoi))
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung trả lời.";
            else if (PhanHoi.Length > 1000)
                TempData["ErrorMessage"] = "Nội dung trả lời tối đa 1000 ký tự.";
            else
            {
                bool sua = !string.IsNullOrWhiteSpace(dg.Phanhoi);
                dg.Phanhoi = PhanHoi;
                dg.Ngayphanhoi = DateTime.Now;
                var maNV = HttpContext.Session.GetString("MaNV")?.Trim();
                dg.Manvphanhoi = string.IsNullOrEmpty(maNV) ? null : maNV;
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = (sua ? "Đã sửa câu trả lời cho đánh giá " : "Đã trả lời đánh giá ") + dg.Madanhgia.Trim()
                        + ". Câu trả lời hiện ngay dưới đánh giá ở trang Đánh giá công khai.";
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = "Không lưu được câu trả lời (đã chạy Database/NangCap_Dot7_DonTrucTuyen_HuyMon.sql chưa?): "
                        + (ex.InnerException?.Message ?? ex.Message);
                }
            }
            return RedirectToAction(nameof(ShowDanhGia), new { loc });
        }

        // POST: DanhGia/XoaTraLoi -> gỡ câu trả lời của nhà hàng
        [KhuVucNhanVien]
        [ChiQuanLy]   // đánh giá chỉ hiện ở tài khoản quản lý
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaTraLoi(string id, string? loc = null)
        {
            var dg = string.IsNullOrWhiteSpace(id) ? null : await _context.DanhGia.FindAsync(id.Trim());
            if (dg != null)
            {
                dg.Phanhoi = null;
                dg.Ngayphanhoi = null;
                dg.Manvphanhoi = null;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã gỡ câu trả lời của đánh giá {dg.Madanhgia.Trim()}.";
            }
            return RedirectToAction(nameof(ShowDanhGia), new { loc });
        }

        // GET: DanhGia/Delete/5
        [KhuVucNhanVien]
        [ChiQuanLy]   // đánh giá chỉ hiện ở tài khoản quản lý
        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID không hợp lệ.");
            }

            var danhGia = await _context.DanhGia.AsNoTracking()
                .FirstOrDefaultAsync(dg => dg.Madanhgia == id.Trim());

            if (danhGia == null)
            {
                return NotFound("Không tìm thấy đánh giá với ID được cung cấp.");
            }

            return View(danhGia);
        }

        // POST: DanhGia/Delete/5
        [KhuVucNhanVien]
        [ChiQuanLy]   // đánh giá chỉ hiện ở tài khoản quản lý
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "ID không hợp lệ.";
                return RedirectToAction(nameof(ShowDanhGia));
            }

            try
            {
                var danhGia = await _context.DanhGia.FindAsync(id.Trim());
                if (danhGia != null)
                {
                    XoaFileAnh(danhGia.Hinhanh);
                    _context.DanhGia.Remove(danhGia);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Xóa đánh giá thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa đánh giá. Đánh giá có thể đã bị xóa hoặc không tồn tại.";
                }
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Lỗi cơ sở dữ liệu: {ex.Message}";
            }

            return RedirectToAction(nameof(ShowDanhGia));
        }

        // ============================================================ helpers

        private IActionResult Loi(string thongBao)
        {
            TempData["ThongBaoLoi"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        private async Task NapDanhSachChonAsync()
        {
            ViewBag.DsMonAn = await _context.MonAns.AsNoTracking().OrderBy(m => m.Tenmon).ToListAsync();
            ViewBag.DsNhanVien = await _context.NhanViens.AsNoTracking().OrderBy(n => n.Hoten).ToListAsync();
        }

        private Task<string> SinhHelper()
            => SinhMaHelper.TaoMaTiepTheoAsync(_context.DanhGia.Select(d => d.Madanhgia), "DG");

        private async Task<string?> LuuAnhAsync(IFormFile file)
        {
            if (file.Length == 0 || file.Length > DungLuongToiDa) return null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_duoiAnh.Contains(ext)) return null;
            if (!(file.ContentType?.StartsWith("image/") ?? false)) return null;

            var thuMuc = Path.Combine(_env.WebRootPath, "Images", "DanhGia");
            Directory.CreateDirectory(thuMuc);
            var ten = $"dg_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
            await using var fs = new FileStream(Path.Combine(thuMuc, ten), FileMode.Create);
            await file.CopyToAsync(fs);
            return ten;
        }

        private void XoaFileAnh(string? ten)
        {
            if (string.IsNullOrWhiteSpace(ten)) return;
            try
            {
                var duongDan = Path.Combine(_env.WebRootPath, "Images", "DanhGia", Path.GetFileName(ten.Trim()));
                if (System.IO.File.Exists(duongDan)) System.IO.File.Delete(duongDan);
            }
            catch { /* không xóa được ảnh thì bỏ qua */ }
        }
    }
}
