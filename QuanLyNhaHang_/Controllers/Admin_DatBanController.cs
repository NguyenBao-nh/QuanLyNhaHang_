using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class Admin_DatBanController : Controller
    {
        private readonly XuLyDatBan _xuLyDatBan;
        private readonly DbQlNhaHangContext _context;

        public Admin_DatBanController(XuLyDatBan xuLyDatBan, DbQlNhaHangContext context)
        {
            _xuLyDatBan = xuLyDatBan;
            _context = context;
        }

        public async Task<IActionResult> DatBan(string? tab = "cho")
        {
            tab = tab is "dang" or "xong" or "tatca" ? tab : "cho";
            List<PhieuDatBan> listMA = await _xuLyDatBan.LayThongTinPhieuDatBanAsync(tab);

            // Thông tin khách hàng / tài khoản của từng phiếu (theo MAKH)
            var dsMaKH = listMA.Where(p => !string.IsNullOrWhiteSpace(p.Makh))
                               .Select(p => p.Makh!.Trim()).Distinct().ToList();
            var dsKhach = await _context.KhachHangs.AsNoTracking()
                .Include(k => k.MataikhoanNavigation)
                .Where(k => dsMaKH.Contains(k.Makh))
                .ToListAsync();
            ViewBag.Khach = dsKhach.ToDictionary(k => k.Makh.Trim(), k => k);

            // Bàn đang có khách nhưng KHÔNG có phiếu đặt trước (khách vãng lai / khách vào thẳng)
            await XuLyBan.DongBoTrangThaiAsync(_context);
            var banCoPhieuDat = (await _context.PhieuDatBans.AsNoTracking()
                    .Where(p => p.Maban != null && p.Trangthai == TrangThaiDatBan.DangPhucVu)
                    .Select(p => p.Maban!).ToListAsync())
                .Select(m => m.Trim()).ToHashSet();
            var khongDatTruoc = (await _context.Bans.AsNoTracking()
                    .Include(b => b.MakhNavigation).ThenInclude(k => k!.MataikhoanNavigation)
                    .Where(b => b.Trangthai == XuLyBan.Day)
                    .ToListAsync())
                .Where(b => !banCoPhieuDat.Contains(b.Maban.Trim()))
                .OrderBy(b => b.Maban.Trim().Length).ThenBy(b => b.Maban.Trim())
                .ToList();

            var soLuong = await _xuLyDatBan.DemTheoTrangThaiAsync();
            soLuong["dang"] += khongDatTruoc.Count;   // tab Đang phục vụ đếm cả khách không đặt trước

            ViewBag.Tab = tab;
            ViewBag.SoLuong = soLuong;
            ViewBag.KhongDatTruoc = tab is "dang" or "tatca" ? khongDatTruoc : new List<Ban>();
            ViewBag.PhieuMo = (await _context.PhieuGoiMons.AsNoTracking()
                    .Where(p => p.Maban != null).Select(p => p.Maban!).ToListAsync())
                .Select(m => m.Trim()).ToHashSet();
            return View(listMA);
        }

        // POST: Admin_DatBan/PhucVuXong -> khách của phiếu đã ăn xong: trả bàn về "Trống", phiếu chuyển sang "Đã phục vụ"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhucVuXong(string maPhieu, string? tab)
        {
            var phieu = await TimPhieuAsync(maPhieu);
            if (phieu == null) return VeDanhSach(tab, "Không tìm thấy phiếu đặt bàn.");

            var maBan = phieu.Maban?.Trim();
            if (!string.IsNullOrEmpty(maBan))
            {
                if (await _context.PhieuGoiMons.AnyAsync(p => p.Maban == maBan))
                    return VeDanhSach(tab, $"Bàn {maBan} còn phiếu gọi món chưa thanh toán. Hãy thanh toán (hoặc xóa phiếu) ở mục Phiếu gọi món trước.");

                var ban = await _context.Bans.FirstOrDefaultAsync(b => b.Maban == maBan);
                if (ban != null) await XuLyBan.GiaiPhongBanAsync(_context, ban);
            }
            phieu.Trangthai = TrangThaiDatBan.DaPhucVu;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã phục vụ xong phiếu {phieu.Maphieu}"
                + (string.IsNullOrEmpty(maBan) ? "." : $", bàn {maBan} đã trống.");
            return RedirectToAction(nameof(DatBan), new { tab });
        }

        // POST: Admin_DatBan/Huy -> khách không đến / hủy: ẩn khỏi danh sách chờ (không xóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(string maPhieu, string? tab)
        {
            var phieu = await TimPhieuAsync(maPhieu);
            if (phieu == null) return VeDanhSach(tab, "Không tìm thấy phiếu đặt bàn.");
            if (phieu.Trangthai == TrangThaiDatBan.DangPhucVu)
                return VeDanhSach(tab, "Phiếu đang phục vụ, hãy bấm \"Phục vụ xong\".");

            phieu.Trangthai = TrangThaiDatBan.DaHuy;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã ẩn phiếu {phieu.Maphieu} khỏi danh sách chờ (xem lại ở tab Đã phục vụ / Đã hủy).";
            return RedirectToAction(nameof(DatBan), new { tab });
        }

        // POST: Admin_DatBan/KhoiPhuc -> đưa phiếu đã ẩn trở lại danh sách chờ xếp bàn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KhoiPhuc(string maPhieu, string? tab)
        {
            var phieu = await TimPhieuAsync(maPhieu);
            if (phieu == null) return VeDanhSach(tab, "Không tìm thấy phiếu đặt bàn.");
            if (phieu.Trangthai == TrangThaiDatBan.DangPhucVu)
                return VeDanhSach(tab, "Phiếu đang phục vụ, không cần khôi phục.");

            phieu.Trangthai = TrangThaiDatBan.ChoXepBan;
            phieu.Maban = null;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã đưa phiếu {phieu.Maphieu} trở lại danh sách chờ xếp bàn.";
            return RedirectToAction(nameof(DatBan), new { tab });
        }

        private Task<PhieuDatBan?> TimPhieuAsync(string? maPhieu)
            => string.IsNullOrWhiteSpace(maPhieu)
                ? Task.FromResult<PhieuDatBan?>(null)
                : _context.PhieuDatBans.FirstOrDefaultAsync(p => p.Maphieu == maPhieu.Trim());

        private IActionResult VeDanhSach(string? tab, string loi)
        {
            TempData["ErrorMessage"] = loi;
            return RedirectToAction(nameof(DatBan), new { tab });
        }
    }
}
