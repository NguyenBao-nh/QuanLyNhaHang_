using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    // Chỉ nhân viên đã đăng nhập; mọi POST phải có mã antiforgery (form trên giao diện)
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class QuanLyBanController : Controller
    {
        private readonly DbQlNhaHangContext _context;

        public QuanLyBanController(DbQlNhaHangContext context)
        {
            _context = context;
        }

        // Sắp xếp theo số thứ tự trong mã bàn (B01, B02, ... B10)
        private static List<Ban> SapXepTheoMa(IEnumerable<Ban> bans) =>
            bans.OrderBy(b => b.Maban.Trim().Length)
                .ThenBy(b => b.Maban.Trim())
                .ToList();

        // Bàn kèm thông tin khách đang ngồi + tài khoản của khách
        private IQueryable<Ban> BanKemKhach() =>
            _context.Bans.AsNoTracking()
                .Include(b => b.MakhNavigation)
                    .ThenInclude(k => k!.MataikhoanNavigation);

        // Thông tin phụ cho bảng: phiếu gọi món đang mở + hóa đơn khách đã đặt về bàn từ lúc ngồi
        private async Task NapThongTinPhuAsync(List<Ban> bans)
        {
            var phieuMo = await _context.PhieuGoiMons.AsNoTracking()
                .Where(p => p.Maban != null)
                .ToListAsync();
            ViewBag.PhieuMo = phieuMo
                .GroupBy(p => p.Maban!.Trim())
                .ToDictionary(g => g.Key, g => g.Select(p => p.Mapgm.Trim()).ToList());

            var donTheoBan = new Dictionary<string, List<HoaDon>>();
            foreach (var b in bans.Where(b => b.Makh != null && b.Giovao != null))
            {
                var ds = await _context.HoaDons.AsNoTracking()
                    .Where(h => h.Maban == b.Maban && h.Makh == b.Makh && h.Ngaylap >= b.Giovao && h.Trangthai != TrangThaiDon.DaHuy)
                    .OrderBy(h => h.Ngaylap)
                    .ToListAsync();
                donTheoBan[b.Maban.Trim()] = ds;
            }
            ViewBag.DonTheoBan = donTheoBan;

            // Phiếu đặt bàn đang phục vụ ở từng bàn (khách đặt trước đã được xếp vào bàn)
            ViewBag.PhieuDatTheoBan = (await _context.PhieuDatBans.AsNoTracking()
                    .Where(p => p.Maban != null && p.Trangthai == TrangThaiDatBan.DangPhucVu)
                    .ToListAsync())
                .GroupBy(p => p.Maban!.Trim())
                .ToDictionary(g => g.Key, g => g.First());
        }

        // GET: QuanLyBan/ShowBan
        public async Task<IActionResult> ShowBan(int? LocBanTrong = 0)
        {
            // Bàn còn phiếu gọi món mà đang "Trống" (dữ liệu lệch) -> tự chuyển "Đầy"
            var daSua = await XuLyBan.DongBoTrangThaiAsync(_context);
            if (daSua.Count > 0)
                ViewBag.DongBo = $"Đã tự sửa trạng thái bàn: {string.Join(", ", daSua)}. "
                    + "Bàn chỉ dùng 2 trạng thái \"Trống\" / \"Đầy\"; bàn còn phiếu gọi món hoặc còn khách thì là \"Đầy\".";

            var bans = await BanKemKhach().ToListAsync();

            if (LocBanTrong == 1)
                bans = bans.Where(b => b.Trangthai?.Trim() == "Trống").ToList();
            else if (LocBanTrong == 2)
                bans = bans.Where(b => b.Trangthai?.Trim() == "Đầy").ToList();

            bans = SapXepTheoMa(bans);
            await NapThongTinPhuAsync(bans);
            return View(bans);
        }

        // GET: QuanLyBan/ShowBanTrong
        public async Task<IActionResult> ShowBanTrong()
        {
            await XuLyBan.DongBoTrangThaiAsync(_context);
            var bans = await _context.Bans.AsNoTracking()
                .Where(b => b.Trangthai == "Trống")
                .ToListAsync();
            return View(SapXepTheoMa(bans));
        }

        // GET: QuanLyBan/XepBan?maBan=B01  hoặc  ?maPhieu=PDB0001 (xếp bàn cho khách đã đặt trước)
        [HttpGet]
        public async Task<IActionResult> XepBan(string? maBan = null, string? maPhieu = null)
        {
            var model = new Ban { Maban = maBan?.Trim() ?? "" };

            if (!string.IsNullOrWhiteSpace(maPhieu))
            {
                var phieu = await _context.PhieuDatBans.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Maphieu == maPhieu.Trim());
                if (phieu != null && !TrangThaiDatBan.LaChoXepBan(phieu.Trangthai))
                {
                    TempData["ErrorMessage"] = $"Phiếu {phieu.Maphieu} đã {phieu.Trangthai?.ToLower()}"
                        + (string.IsNullOrWhiteSpace(phieu.Maban) ? "." : $" (bàn {phieu.Maban.Trim()}).");
                    phieu = null;
                }
                if (phieu != null)
                {
                    model.Makh = phieu.Makh?.Trim();
                    model.Tenkhach = phieu.Tenkh;
                    model.Sdtkhach = phieu.Sdt;
                    ViewBag.Phieu = phieu;
                }
            }

            await NapDuLieuXepBanAsync();
            return View(model);
        }

        // POST: QuanLyBan/XepBan -> khách nhận bàn (gắn khách / tài khoản vào bàn, đổi sang "Đầy")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XepBan(string Maban, string? Makh, string? Tenkhach, string? Sdtkhach, string? maPhieu)
        {
            Maban = Maban?.Trim() ?? "";
            Makh = string.IsNullOrWhiteSpace(Makh) ? null : Makh.Trim();
            Tenkhach = Tenkhach?.Trim();
            Sdtkhach = Sdtkhach?.Trim();

            var ban = await _context.Bans.FirstOrDefaultAsync(b => b.Maban == Maban);
            string? loi = null;

            if (ban == null)
                loi = "Vui lòng chọn bàn.";
            else if (ban.Trangthai?.Trim() == "Đầy")
                loi = $"Bàn {Maban} đang có khách.";
            else if (await XuLyBan.ConPhieuGoiMonAsync(_context, Maban))
                loi = $"Bàn {Maban} còn phiếu gọi món chưa thanh toán, không thể xếp khách khác.";

            KhachHang? kh = null;
            if (loi == null && Makh != null)
            {
                kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Makh == Makh);
                if (kh == null)
                    loi = "Khách hàng không tồn tại.";
                else
                {
                    var banKhac = await _context.Bans.AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Makh == Makh && b.Maban != Maban && b.Trangthai == "Đầy");
                    if (banKhac != null)
                        loi = $"Khách {kh.Hoten} đang ngồi ở bàn {banKhac.Maban.Trim()}. Hãy trả bàn đó trước.";
                }
            }
            if (loi == null && kh == null && string.IsNullOrWhiteSpace(Tenkhach))
                loi = "Chọn tài khoản khách hàng hoặc nhập tên khách vãng lai.";

            // Xếp bàn cho phiếu đặt trước: phiếu phải còn đang chờ
            PhieuDatBan? phieuDat = null;
            if (!string.IsNullOrWhiteSpace(maPhieu))
            {
                phieuDat = await _context.PhieuDatBans.FirstOrDefaultAsync(p => p.Maphieu == maPhieu.Trim());
                if (loi == null && phieuDat != null && !TrangThaiDatBan.LaChoXepBan(phieuDat.Trangthai))
                    loi = $"Phiếu {phieuDat.Maphieu} đã được xử lý ({phieuDat.Trangthai}).";
            }

            if (loi != null)
            {
                TempData["ErrorMessage"] = loi;
                await NapDuLieuXepBanAsync();
                if (phieuDat != null) ViewBag.Phieu = phieuDat;
                return View(new Ban { Maban = Maban, Makh = Makh, Tenkhach = Tenkhach, Sdtkhach = Sdtkhach });
            }

            ban!.Trangthai = "Đầy";
            ban.Makh = kh?.Makh;
            ban.Tenkhach = kh?.Hoten ?? Tenkhach;
            ban.Sdtkhach = string.IsNullOrWhiteSpace(Sdtkhach) ? kh?.Sodt?.Trim() : Sdtkhach;
            ban.Giovao = DateTime.Now;

            // Phiếu đặt bàn -> "Đang phục vụ": tự rời khỏi danh sách chờ xếp bàn, không gây nhầm lẫn
            if (phieuDat != null)
            {
                phieuDat.Trangthai = TrangThaiDatBan.DangPhucVu;
                phieuDat.Maban = Maban;
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xếp {ban.Tenkhach} vào bàn {Maban}."
                + (phieuDat != null ? $" Phiếu đặt {phieuDat.Maphieu} chuyển sang Đang phục vụ." : "")
                + (kh != null ? " Món khách gọi trên web sẽ tự gắn về bàn này." : "");
            return RedirectToAction(nameof(ShowBan));
        }

        // POST: trước đây là link GET nên gõ URL cũng đổi được trạng thái bàn
        [HttpPost]
        public Task<IActionResult> ChangeTrangThai(string maBan, string newTrangThai)
            => DoiTrangThaiAsync(maBan, newTrangThai, nameof(ShowBan));

        [HttpPost]
        public Task<IActionResult> ChangeTrangThaiTrong(string maBan, string newTrangThai)
            => DoiTrangThaiAsync(maBan, newTrangThai, nameof(ShowBanTrong));

        private async Task<IActionResult> DoiTrangThaiAsync(string maBan, string newTrangThai, string actionVe)
        {
            // Đặt bàn giờ phải chọn khách -> chuyển sang trang Xếp bàn
            if (newTrangThai?.Trim() == "Đầy")
                return RedirectToAction(nameof(XepBan), new { maBan = maBan?.Trim() });

            var ban = string.IsNullOrWhiteSpace(maBan)
                ? null
                : await _context.Bans.FirstOrDefaultAsync(b => b.Maban == maBan.Trim());

            if (ban == null || string.IsNullOrWhiteSpace(newTrangThai))
            {
                TempData["ErrorMessage"] = "Cập nhật trạng thái bàn thất bại!";
            }
            else if (newTrangThai.Trim() == XuLyBan.Trong && await XuLyBan.ConPhieuGoiMonAsync(_context, ban.Maban))
            {
                TempData["ErrorMessage"] = $"Bàn {ban.Maban.Trim()} còn phiếu gọi món chưa thanh toán, không thể trả bàn. "
                    + "Hãy thanh toán (thanh toán xong bàn tự trống) hoặc xóa phiếu trước.";
            }
            else
            {
                if (newTrangThai.Trim() == XuLyBan.Trong)
                    await XuLyBan.GiaiPhongBanAsync(_context, ban);
                else
                    ban.Trangthai = newTrangThai.Trim();
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật trạng thái bàn thành công!";
            }
            return RedirectToAction(actionVe);
        }

        // POST: QuanLyBan/PhucVuXong -> bàn đang phục vụ đã xong: bàn trở về "Trống",
        // xóa khách khỏi bàn, phiếu đặt bàn của bàn này chuyển sang "Đã phục vụ" (ẩn khỏi danh sách chờ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhucVuXong(string maBan, int? LocBanTrong, string? veDatBan = null)
        {
            maBan = maBan?.Trim() ?? "";
            var ban = maBan == "" ? null : await _context.Bans.FirstOrDefaultAsync(b => b.Maban == maBan);

            if (ban == null)
                TempData["ErrorMessage"] = "Không tìm thấy bàn.";
            else if (await _context.PhieuGoiMons.AnyAsync(p => p.Maban == maBan))
                TempData["ErrorMessage"] = $"Bàn {maBan} còn phiếu gọi món chưa thanh toán. "
                    + "Hãy thanh toán ở mục Phiếu gọi món (thanh toán xong bàn tự trống) hoặc xóa phiếu trước.";
            else
            {
                var ten = ban.Tenkhach;
                await XuLyBan.GiaiPhongBanAsync(_context, ban);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Bàn {maBan} đã phục vụ xong"
                    + (string.IsNullOrWhiteSpace(ten) ? "" : $" (khách {ten})") + ", bàn đã trống.";
            }
            // Bấm từ trang Khách đặt bàn (tab Đang phục vụ) thì quay lại đó
            if (!string.IsNullOrEmpty(veDatBan))
                return RedirectToAction("DatBan", "Admin_DatBan", new { tab = veDatBan });
            return RedirectToAction(nameof(ShowBan), new { LocBanTrong });
        }

        // POST: QuanLyBan/ThemBan -> thêm bàn mới (mở rộng quy mô), mã tự sinh tiếp theo: B11, B12, ...
        [HttpPost]
        public async Task<IActionResult> ThemBan(int soLuong = 1)
        {
            if (soLuong < 1 || soLuong > 20)
            {
                TempData["ErrorMessage"] = "Mỗi lần thêm từ 1 đến 20 bàn.";
                return RedirectToAction(nameof(ShowBan));
            }

            // Số lớn nhất trong các mã dạng B + số (B01, B10, B11...)
            var cacMa = await _context.Bans.AsNoTracking().Select(b => b.Maban).ToListAsync();
            int max = 0;
            foreach (var m in cacMa.Select(x => x.Trim()))
                if (m.Length > 1 && (m[0] == 'B' || m[0] == 'b') && int.TryParse(m.AsSpan(1), out int so) && so > max)
                    max = so;

            var maMoi = new List<string>();
            for (int i = 1; i <= soLuong; i++)
            {
                var ma = "B" + (max + i).ToString("D2");
                if (ma.Length > 10) break;   // MABAN char(10)
                _context.Bans.Add(new Ban { Maban = ma, Trangthai = XuLyBan.Trong });
                maMoi.Add(ma);
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm {maMoi.Count} bàn: {string.Join(", ", maMoi)}.";
            return RedirectToAction(nameof(ShowBan));
        }

        // POST: QuanLyBan/XoaBan -> chỉ xóa được bàn chưa từng dùng (thêm nhầm). Bàn đã có hóa đơn giữ lại để không mất lịch sử.
        [HttpPost]
        public async Task<IActionResult> XoaBan(string maBan, int? LocBanTrong)
        {
            maBan = maBan?.Trim() ?? "";
            var ban = maBan == "" ? null : await _context.Bans.FirstOrDefaultAsync(b => b.Maban == maBan);
            string? loi = null;

            if (ban == null)
                loi = "Không tìm thấy bàn.";
            else if (ban.Trangthai?.Trim() != XuLyBan.Trong)
                loi = $"Bàn {maBan} đang có khách, không thể xóa.";
            else if (await XuLyBan.ConPhieuGoiMonAsync(_context, maBan))
                loi = $"Bàn {maBan} còn phiếu gọi món, không thể xóa.";
            else if (await _context.PhieuDatBans.AnyAsync(p => p.Maban == maBan && p.Trangthai == TrangThaiDatBan.DangPhucVu))
                loi = $"Bàn {maBan} đang được xếp cho khách đặt trước, không thể xóa.";
            else
            {
                int soHD = await _context.HoaDons.CountAsync(h => h.Maban == maBan);
                if (soHD > 0)
                    loi = $"Bàn {maBan} đã có {soHD} hóa đơn (lịch sử bán hàng) nên không xóa được, để không mất dữ liệu hóa đơn.";
            }

            if (loi != null)
                TempData["ErrorMessage"] = loi;
            else
            {
                _context.Bans.Remove(ban!);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa bàn {maBan}.";
            }
            return RedirectToAction(nameof(ShowBan), new { LocBanTrong });
        }

        public static void XoaKhachKhoiBan(Ban ban)
        {
            ban.Makh = null;
            ban.Tenkhach = null;
            ban.Sdtkhach = null;
            ban.Giovao = null;
        }

        private async Task NapDuLieuXepBanAsync()
        {
            await XuLyBan.DongBoTrangThaiAsync(_context);
            var bans = await _context.Bans.AsNoTracking().Where(b => b.Trangthai == "Trống").ToListAsync();
            ViewBag.BanTrong = SapXepTheoMa(bans);

            ViewBag.DsKhach = await _context.KhachHangs.AsNoTracking()
                .Include(k => k.MataikhoanNavigation)
                .OrderBy(k => k.Hoten)
                .ToListAsync();

            // Phiếu đặt bàn còn chờ xếp (từ hôm qua trở đi), phiếu đã xếp / đã phục vụ không hiện nữa
            var homQua = DateTime.Today.AddDays(-1);
            ViewBag.PhieuCho = await _context.PhieuDatBans.AsNoTracking()
                .Where(p => (p.Trangthai == null || p.Trangthai == "" || p.Trangthai == TrangThaiDatBan.ChoXepBan)
                         && (p.Ngaydat == null || p.Ngaydat >= homQua))
                .OrderBy(p => p.Ngaydat).ThenBy(p => p.Giodat)
                .ToListAsync();

            // Khách đang ngồi ở bàn nào (để cảnh báo trong danh sách chọn)
            ViewBag.KhachDangNgoi = (await _context.Bans.AsNoTracking()
                    .Where(b => b.Makh != null && b.Trangthai == "Đầy").ToListAsync())
                .GroupBy(b => b.Makh!.Trim())
                .ToDictionary(g => g.Key, g => g.First().Maban.Trim());
        }
    }
}
