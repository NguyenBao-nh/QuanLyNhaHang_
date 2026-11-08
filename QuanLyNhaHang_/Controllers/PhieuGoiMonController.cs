using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    // Chỉ nhân viên đã đăng nhập; mọi POST phải có mã antiforgery (form trên giao diện)
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class PhieuGoiMonController : Controller
    {
        private readonly DbQlNhaHangContext _context;
        private readonly XuLyPhieuGoiMon _xuLyPhieuGoiMon;
        private readonly XuLyThongTinMonAn _xuLyMonAn;
        private readonly XuLyThongTinKhachHang _xuLyKhachHang;
        private readonly XuLyThanhToan _xuLyThanhToan;
        private readonly XuLyMaGiamGia _xuLyMaGiamGia;
        private readonly XuLyKet _xuLyKet;

        public PhieuGoiMonController(
            DbQlNhaHangContext context,
            XuLyPhieuGoiMon xuLyPhieuGoiMon,
            XuLyThongTinMonAn xuLyMonAn,
            XuLyThongTinKhachHang xuLyKhachHang,
            XuLyThanhToan xuLyThanhToan,
            XuLyMaGiamGia xuLyMaGiamGia,
            XuLyKet xuLyKet)
        {
            _xuLyKet = xuLyKet;
            _context = context;
            _xuLyPhieuGoiMon = xuLyPhieuGoiMon;
            _xuLyMonAn = xuLyMonAn;
            _xuLyKhachHang = xuLyKhachHang;
            _xuLyThanhToan = xuLyThanhToan;
            _xuLyMaGiamGia = xuLyMaGiamGia;
        }

        public IActionResult Index() => RedirectToAction(nameof(DanhSachPhieuGoiMon));

        // GET: PhieuGoiMon/DanhSachPhieuGoiMon
        public async Task<IActionResult> DanhSachPhieuGoiMon()
        {
            await DongBoBanAsync();
            var list = await _xuLyPhieuGoiMon.LayThongTinPhieuGoiMonAsync();
            ViewBag.BanKhach = await LayBanKemKhachAsync();

            // Số món của từng phiếu (phiếu chưa có món -> nhắc chọn món)
            ViewBag.SoMon = (await _context.ChiTietGoiMons.AsNoTracking()
                    .Select(c => new { c.Mapgm, c.Soluong }).ToListAsync())
                .GroupBy(c => c.Mapgm.Trim())
                .ToDictionary(g => g.Key, g => g.Sum(c => c.Soluong ?? 0));
            return View(list);
        }

        // GET: PhieuGoiMon/ThemPhieuGoiMon
        public async Task<IActionResult> ThemPhieuGoiMon()
        {
            await NapDuLieuChonBanAsync();
            ViewBag.MaP = await SinhMaPhieuAsync();
            return View();
        }

        // GET: PhieuGoiMon/ChiTietPhieu?ma=PGM0001&maBan=1
        public async Task<IActionResult> ChiTietPhieu(string ma, string maBan)
        {
            ViewBag.MaPGM = ma;
            ViewBag.MaBan = maBan;

            // Khách của phiếu (lưu trên phiếu)
            ViewBag.Phieu = string.IsNullOrWhiteSpace(ma) ? null
                : await _context.PhieuGoiMons.AsNoTracking()
                    .Include(p => p.MakhNavigation).ThenInclude(k => k!.MataikhoanNavigation)
                    .FirstOrDefaultAsync(p => p.Mapgm == ma.Trim());

            // Khách đang ngồi ở bàn (tài khoản đặt món về bàn này)
            ViewBag.BanInfo = string.IsNullOrWhiteSpace(maBan) ? null
                : await _context.Bans.AsNoTracking()
                    .Include(b => b.MakhNavigation).ThenInclude(k => k!.MataikhoanNavigation)
                    .FirstOrDefaultAsync(b => b.Maban == maBan.Trim());

            var listCT = await _xuLyPhieuGoiMon.GetChiTietPhieuGoiMonAsync(ma);
            ViewBag.dsMonAn = listCT;

            // Món đã hủy của phiếu này (khách đợi lâu / không dùng nữa)
            var maPhieu = ma?.Trim() ?? "";
            // Ca đang mở (thanh toán tại quán bắt buộc đã mở ca)
            ViewBag.CaDangMo = await _xuLyKet.LayCaDangMoAsync();

            ViewBag.MonHuy = await _context.HuyMons.AsNoTracking()
                .Where(h => h.Mapgm == maPhieu)
                .OrderByDescending(h => h.Thoigian)
                .ToListAsync();

            // Phụ phí / bồi thường đã thêm vào phiếu này (khách làm vỡ bát, hỏng đồ...) - cộng vào tổng hóa đơn
            var phuPhiList = await _context.PhuPhis.AsNoTracking()
                .Where(p => p.Mapgm == maPhieu)
                .OrderByDescending(p => p.Thoigian)
                .ToListAsync();
            ViewBag.PhuPhiList = phuPhiList;
            int tongPhuPhi = phuPhiList.Sum(p => p.Sotien);
            ViewBag.TongPhuPhi = tongPhuPhi;
            ViewBag.TongHoaDon = listCT.Sum(ct => ct.ThanhTien) + tongPhuPhi;
            return View(listCT);
        }

        // POST: tạo phiếu mới cho bàn rồi hiển thị danh sách món để chọn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemPhieu(string MaBan, string? MaKH, string? TenKhach, string? SdtKhach)
        {
            MaBan = MaBan?.Trim() ?? "";
            MaKH = NullNeuRong(MaKH);

            var banChon = await LayBanTrongAsync();
            var ban = await _context.Bans.FirstOrDefaultAsync(b => b.Maban == MaBan);
            string? loi = null;
            if (ban == null || !banChon.Any(b => b.Maban.Trim() == MaBan))
                loi = "Bàn không tồn tại hoặc đang có phiếu gọi món chưa thanh toán!";

            KhachHang? kh = null;
            if (loi == null && MaKH != null)
            {
                kh = await _context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.Makh == MaKH);
                if (kh == null) loi = "Khách hàng không tồn tại!";
                else if (await _context.Bans.AnyAsync(b => b.Makh == MaKH && b.Maban != MaBan && b.Trangthai == "Đầy"))
                    loi = $"Khách {kh.Hoten} đang ngồi ở bàn khác!";
            }

            // Khách của phiếu: tài khoản được chọn > khách đang ngồi ở bàn > tên khách vãng lai nhập tay
            if (loi == null && kh == null && !string.IsNullOrWhiteSpace(ban!.Makh))
                kh = await _context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.Makh == ban.Makh);

            string? tenKhach = kh?.Hoten ?? NullNeuRong(TenKhach) ?? NullNeuRong(ban?.Tenkhach);
            string? sdtKhach = NullNeuRong(kh?.Sodt) ?? NullNeuRong(SdtKhach) ?? NullNeuRong(ban?.Sdtkhach);

            if (loi == null && kh == null && tenKhach == null)
                loi = "Vui lòng chọn tài khoản khách hàng hoặc nhập tên khách vãng lai cho phiếu.";

            if (loi != null)
            {
                TempData["ErrorMessage"] = loi;
                await NapDuLieuChonBanAsync();
                ViewBag.MaP = await SinhMaPhieuAsync();
                return View("ThemPhieuGoiMon");
            }

            // Gắn khách (tài khoản) vào bàn: món gọi tại bàn và món khách tự đặt trên web đều về đúng bàn này
            if (kh != null)
            {
                if (ban!.Makh?.Trim() != kh.Makh.Trim()) ban.Giovao = DateTime.Now;
                ban.Makh = kh.Makh;
                ban.Tenkhach = kh.Hoten;
                ban.Sdtkhach = kh.Sodt?.Trim();
            }
            else if (ban!.Makh == null)
            {
                ban.Tenkhach = tenKhach;
                ban.Sdtkhach = sdtKhach;
                ban.Giovao ??= DateTime.Now;
            }

            // Có phiếu gọi món là bàn đang phục vụ -> "Đầy" ngay (không đợi bước lưu món)
            ban!.Trangthai = XuLyBan.Day;
            ban.Giovao ??= DateTime.Now;

            var phieu = new PhieuGoiMon
            {
                Mapgm = await SinhMaPhieuAsync(),
                Maban = MaBan,
                Manv = HttpContext.Session.GetString("MaNV"),
                Ngaylap = DateTime.Now,
                Trangthai = "Đang phục vụ",
                Makh = kh?.Makh,
                Tenkhach = tenKhach,
                Sdtkhach = sdtKhach
            };
            _context.PhieuGoiMons.Add(phieu);

            // Mã phiếu = số lớn nhất + 1, nên 2 yêu cầu cùng lúc (bấm "Thêm Phiếu" 2 lần, 2 máy cùng tạo)
            // có thể sinh trùng mã -> lỗi PRIMARY KEY. Bắt lỗi trùng khóa và xử lý lại thay vì văng lỗi.
            for (int lan = 1; ; lan++)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException ex) when (LaLoiTrungKhoa(ex) && lan <= 5)
                {
                    _context.Entry(phieu).State = EntityState.Detached;

                    // Yêu cầu kia đã tạo phiếu cho chính bàn này -> dùng luôn phiếu đó (không tạo 2 phiếu cho 1 bàn)
                    var phieuDaCo = await _context.PhieuGoiMons.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Maban == MaBan);
                    if (phieuDaCo != null)
                        return RedirectToAction(nameof(ChonMon), new { ma = phieuDaCo.Mapgm.Trim() });

                    // Trùng mã với phiếu của bàn khác -> sinh mã mới rồi lưu lại
                    phieu.Mapgm = await SinhMaPhieuAsync();
                    _context.PhieuGoiMons.Add(phieu);
                }
            }

            // Chuyển sang trang chọn món bằng GET: bấm F5 không gửi lại form tạo phiếu
            return RedirectToAction(nameof(ChonMon), new { ma = phieu.Mapgm.Trim() });
        }

        // Lỗi SQL Server 2627 / 2601 = trùng PRIMARY KEY / UNIQUE
        private static bool LaLoiTrungKhoa(DbUpdateException ex)
            => ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && (sql.Number == 2627 || sql.Number == 2601);

        // GET: PhieuGoiMon/ChonMon?ma=PGM0001 -> chọn món cho phiếu đã có (phiếu chưa có món / gọi thêm món)
        public async Task<IActionResult> ChonMon(string ma)
        {
            var phieu = string.IsNullOrWhiteSpace(ma) ? null
                : await _context.PhieuGoiMons.AsNoTracking().FirstOrDefaultAsync(p => p.Mapgm == ma.Trim());
            if (phieu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu gọi món (có thể đã thanh toán).";
                return RedirectToAction(nameof(DanhSachPhieuGoiMon));
            }
            bool coMon = await _context.ChiTietGoiMons.AnyAsync(c => c.Mapgm == phieu.Mapgm);
            return await HienChonMonAsync(phieu, goiThem: coMon);
        }

        private async Task<IActionResult> HienChonMonAsync(PhieuGoiMon phieu, bool goiThem)
        {
            var maBan = phieu.Maban?.Trim() ?? "";
            ViewBag.PhieuMoi = phieu;
            ViewBag.GoiThem = goiThem;
            ViewBag.BanMoi = await _context.Bans.AsNoTracking()
                .Include(b => b.MakhNavigation).ThenInclude(k => k!.MataikhoanNavigation)
                .FirstOrDefaultAsync(b => b.Maban == maBan);
            ViewBag.dsMonAn = await _xuLyMonAn.LayThongTinMonAnAsync();
            ViewBag.MonDaGoi = await _xuLyPhieuGoiMon.GetChiTietPhieuGoiMonAsync(phieu.Mapgm);
            return View("ThemPhieuGoiMon");
        }

        // POST: lưu danh sách món vào phiếu (cộng dồn nếu món đã có), bàn của phiếu luôn "Đầy"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemChiTietPhieuGoiMon(string MaPhieu, string? MaBan, string[] MaMonAn, int[] SoLuong)
        {
            MaPhieu = MaPhieu?.Trim() ?? "";
            var phieu = await _context.PhieuGoiMons.FirstOrDefaultAsync(p => p.Mapgm == MaPhieu);
            if (phieu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu gọi món (có thể đã thanh toán hoặc bị xóa).";
                return RedirectToAction(nameof(DanhSachPhieuGoiMon));
            }

            // Gộp món trùng + bỏ số lượng <= 0 (khóa chính là MaPGM + MaMonAn)
            var items = MaMonAn.Zip(SoLuong, (ma, sl) => (Ma: ma?.Trim() ?? "", Sl: sl))
                .Where(x => x.Ma != "" && x.Sl > 0)
                .GroupBy(x => x.Ma)
                .Select(g => (Ma: g.Key, Sl: g.Sum(x => x.Sl)))
                .ToList();

            if (items.Count == 0)
            {
                // Giữ nguyên phiếu, quay lại bước chọn món của ĐÚNG phiếu này (trước đây bị bỏ lại phiếu rỗng)
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một món!";
                return RedirectToAction(nameof(ChonMon), new { ma = MaPhieu });
            }

            var daCo = await _context.ChiTietGoiMons.Where(c => c.Mapgm == MaPhieu).ToListAsync();
            foreach (var (ma, sl) in items)
            {
                var cu = daCo.FirstOrDefault(c => c.Mamonan.Trim() == ma);
                if (cu != null) cu.Soluong = (cu.Soluong ?? 0) + sl;
                else _context.ChiTietGoiMons.Add(new ChiTietGoiMon { Mapgm = MaPhieu, Mamonan = ma, Soluong = sl });
            }

            // Bàn lấy theo phiếu (không tin mã bàn gửi từ form)
            var maBan = phieu.Maban?.Trim() ?? MaBan?.Trim() ?? "";
            var ban = await _context.Bans.FirstOrDefaultAsync(b => b.Maban == maBan);
            if (ban != null)
            {
                ban.Trangthai = XuLyBan.Day;
                ban.Giovao ??= DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã lưu món cho phiếu {MaPhieu} (bàn {maBan}).";
            return RedirectToAction(nameof(DanhSachPhieuGoiMon));
        }

        // POST: hủy món (một phần hoặc toàn bộ số lượng) - khách đợi lâu / không muốn dùng nữa.
        // Món hủy bị trừ khỏi phiếu (không tính tiền) và được ghi vào bảng HuyMon để tra cứu.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyMonAn(string MaPhieu, string MaMonAn, int SoLuongHuy, string? LyDo)
        {
            MaPhieu = MaPhieu?.Trim() ?? "";
            MaMonAn = MaMonAn?.Trim() ?? "";

            var phieu = await _context.PhieuGoiMons.AsNoTracking().FirstOrDefaultAsync(p => p.Mapgm == MaPhieu);
            if (phieu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu gọi món (có thể đã thanh toán hoặc bị xóa).";
                return RedirectToAction(nameof(DanhSachPhieuGoiMon));
            }
            var maBan = phieu.Maban?.Trim();

            var ct = await _context.ChiTietGoiMons.FirstOrDefaultAsync(c => c.Mapgm == MaPhieu && c.Mamonan == MaMonAn);
            int dangCo = ct?.Soluong ?? 0;

            // Chỉ nhận lý do nằm trong danh sách gợi ý sẵn (đã bỏ "Lý do khác" tự nhập)
            var lyDo = NullNeuRong(LyDo);

            string? loi = null;
            if (ct == null || dangCo <= 0) loi = "Món này không còn trong phiếu.";
            else if (SoLuongHuy < 1 || SoLuongHuy > dangCo) loi = $"Số lượng hủy phải từ 1 đến {dangCo}.";
            else if (lyDo == null || !LyDoHuyMon.GoiY.Contains(lyDo)) loi = "Vui lòng chọn một lý do hủy món hợp lệ.";

            if (loi != null)
            {
                TempData["ErrorMessage"] = loi;
                return RedirectToAction(nameof(ChiTietPhieu), new { ma = MaPhieu, maBan });
            }

            var tenMon = await _context.MonAns.AsNoTracking()
                .Where(m => m.Mamonan == MaMonAn).Select(m => m.Tenmon).FirstOrDefaultAsync();

            if (SoLuongHuy >= dangCo) _context.ChiTietGoiMons.Remove(ct!);
            else ct!.Soluong = dangCo - SoLuongHuy;

            _context.HuyMons.Add(new HuyMon
            {
                Mapgm = MaPhieu,
                Maban = maBan,
                Mamonan = MaMonAn,
                Tenmon = tenMon,
                Soluong = SoLuongHuy,
                Lydo = lyDo,
                Manv = NullNeuRong(HttpContext.Session.GetString("MaNV")),
                Thoigian = DateTime.Now
            });

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã hủy {SoLuongHuy} x {tenMon ?? MaMonAn} (lý do: {lyDo}). Món hủy không tính vào hóa đơn.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = "Không hủy được món (đã chạy Database/NangCap_Dot7_DonTrucTuyen_HuyMon.sql chưa?): "
                    + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToAction(nameof(ChiTietPhieu), new { ma = MaPhieu, maBan });
        }

        // POST: thêm phụ phí / bồi thường vào phiếu (khách làm vỡ bát, hỏng đồ...) - cộng vào tổng hóa đơn khi thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemPhuPhi(string MaPhieu, string? MaBan, string? NoiDung, int SoTien)
        {
            MaPhieu = MaPhieu?.Trim() ?? "";
            var phieu = await _context.PhieuGoiMons.AsNoTracking().FirstOrDefaultAsync(p => p.Mapgm == MaPhieu);
            if (phieu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu gọi món (có thể đã thanh toán hoặc bị xóa).";
                return RedirectToAction(nameof(DanhSachPhieuGoiMon));
            }
            var maBan = phieu.Maban?.Trim() ?? NullNeuRong(MaBan);
            var noiDung = NullNeuRong(NoiDung);

            string? loi = null;
            if (noiDung == null) loi = "Vui lòng ghi lý do thu phụ phí / bồi thường (VD: khách làm vỡ 2 bát).";
            else if (noiDung.Length > 200) loi = "Lý do tối đa 200 ký tự.";
            else if (SoTien <= 0) loi = "Số tiền phụ phí phải lớn hơn 0.";

            if (loi != null)
            {
                TempData["ErrorMessage"] = loi;
                return RedirectToAction(nameof(ChiTietPhieu), new { ma = MaPhieu, maBan });
            }

            _context.PhuPhis.Add(new PhuPhi
            {
                Mapgm = MaPhieu,
                Maban = maBan,
                Noidung = noiDung!,
                Sotien = SoTien,
                Manv = NullNeuRong(HttpContext.Session.GetString("MaNV")),
                Thoigian = DateTime.Now
            });

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã thêm phụ phí {SoTien:N0}đ ({noiDung}) - đã cộng vào tổng hóa đơn của bàn.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = "Không lưu được phụ phí (đã chạy Database/NangCap_PhuPhiBoiThuong.sql chưa?): "
                    + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToAction(nameof(ChiTietPhieu), new { ma = MaPhieu, maBan });
        }

        // POST: xóa khoản phụ phí ghi nhầm (chỉ xóa được khi phiếu chưa thanh toán)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaPhuPhi(int id)
        {
            var pp = await _context.PhuPhis.FirstOrDefaultAsync(p => p.Maphuphi == id);
            if (pp == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khoản phụ phí.";
                return RedirectToAction(nameof(DanhSachPhieuGoiMon));
            }
            var maP = pp.Mapgm;
            var maBan = pp.Maban;
            bool conPhieu = await _context.PhieuGoiMons.AnyAsync(p => p.Mapgm == maP);
            if (!conPhieu)
            {
                TempData["ErrorMessage"] = "Phiếu đã thanh toán, không xóa được khoản phụ phí này nữa.";
                return RedirectToAction(nameof(DanhSachPhieuGoiMon));
            }
            _context.PhuPhis.Remove(pp);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa khoản phụ phí.";
            return RedirectToAction(nameof(ChiTietPhieu), new { ma = maP, maBan });
        }

        // GET: PhieuGoiMon/LichSuHuyMon?ngay=2026-11-07  -> danh sách món đã hủy (mặc định 7 ngày gần nhất)
        public async Task<IActionResult> LichSuHuyMon(string? ngay = null)
        {
            var q = _context.HuyMons.AsNoTracking().AsQueryable();
            if (DateTime.TryParse(ngay, out var d))
            {
                var den = d.Date.AddDays(1);
                q = q.Where(h => h.Thoigian >= d.Date && h.Thoigian < den);
                ViewBag.Ngay = d.ToString("yyyy-MM-dd");
            }
            else
            {
                var tu = DateTime.Today.AddDays(-6);
                q = q.Where(h => h.Thoigian >= tu);
            }

            var ds = await q.OrderByDescending(h => h.Thoigian).ToListAsync();
            var maNVs = ds.Select(h => h.Manv?.Trim()).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();
            ViewBag.TenNV = (await _context.NhanViens.AsNoTracking().Where(n => maNVs.Contains(n.Manv)).ToListAsync())
                .ToDictionary(n => n.Manv.Trim(), n => n.Hoten ?? n.Manv.Trim());
            return View(ds);
        }

        // POST: chỉ xóa khi bấm nút Xóa (gõ URL /PhieuGoiMon/XoaPhieu?ma=... không xóa được)
        [HttpPost]
        public async Task<IActionResult> XoaPhieu(string ma)
        {
            var maBan = string.IsNullOrWhiteSpace(ma) ? null
                : await _context.PhieuGoiMons.AsNoTracking()
                    .Where(p => p.Mapgm == ma.Trim()).Select(p => p.Maban).FirstOrDefaultAsync();
            await XoaPhieuVaChiTietAsync(ma);
            TempData["SuccessMessage"] = $"Đã xóa phiếu {ma?.Trim()}."
                + (string.IsNullOrWhiteSpace(maBan) ? ""
                   : $" Bàn {maBan.Trim()} vẫn giữ khách; nếu khách đã về hãy bấm \"Phục vụ xong\" ở Quản lý bàn.");
            return RedirectToAction(nameof(DanhSachPhieuGoiMon));
        }

        // POST: xem trước hóa đơn trước khi thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XuatHoaDon(IFormCollection f)
        {
            string maP = f["MaPhieu"].ToString();
            string sdt = f["SDT_Khach"].ToString().Trim();
            string maGG = f["MaGiamGia"].ToString().Trim();
            string maBanForm = f["MaBan"].ToString().Trim();

            // Ưu tiên SĐT nhập tay; không nhập thì lấy khách (tài khoản) đang ngồi ở bàn
            KhachHang? kh = sdt == "" ? null : await _xuLyKhachHang.LayThongTinTuSDTAsync(sdt);
            // Không nhập SĐT -> lấy khách đã lưu trên phiếu gọi món
            var phieuGM = maP == "" ? null : await _context.PhieuGoiMons.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Mapgm == maP.Trim());
            if (kh == null && sdt == "" && !string.IsNullOrWhiteSpace(phieuGM?.Makh))
                kh = await _context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.Makh == phieuGM.Makh);
            ViewBag.TenKhachPhieu = phieuGM?.Tenkhach;
            ViewBag.SdtKhachPhieu = phieuGM?.Sdtkhach;

            if (kh == null && maBanForm != "")
            {
                var maKHBan = await _context.Bans.AsNoTracking()
                    .Where(b => b.Maban == maBanForm).Select(b => b.Makh).FirstOrDefaultAsync();
                if (!string.IsNullOrWhiteSpace(maKHBan))
                    kh = await _context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.Makh == maKHBan);
            }

            var tinh = await TinhHoaDonAsync(f["MaMonAn[]"].ToArray()!, f["SoLuong[]"].ToArray()!, maGG);

            // Phụ phí / bồi thường của phiếu (nếu có) - cộng thêm, không bị mã giảm giá áp dụng
            var phuPhiList = maP == "" ? new List<PhuPhi>()
                : await _context.PhuPhis.AsNoTracking().Where(p => p.Mapgm == maP.Trim()).ToListAsync();
            int tongPhuPhi = phuPhiList.Sum(p => p.Sotien);

            var hoaDon = new HoaDon
            {
                Mahoadon = await SinhMaHoaDonAsync(),
                Maban = f["MaBan"].ToString(),
                Makh = kh?.Makh,
                Manv = NullNeuRong(f["MaNV"]) ?? HttpContext.Session.GetString("MaNV"),
                Magiamgia = tinh.MaHopLe,
                Ngaylap = DateTime.Now,
                // Chỉ 2 hình thức: Tiền mặt (cộng vào két) / Chuyển khoản
                Hinhthuc = HinhThucThanhToan.LaTienMat(f["PhuongThuc"]) ? HinhThucThanhToan.TienMat : HinhThucThanhToan.ChuyenKhoan,
                Tongtien = tinh.SauGiam + tongPhuPhi
            };
            foreach (var ct in tinh.ChiTiet) ct.Mahoadon = hoaDon.Mahoadon;

            var view = new ViewThanhToan
            {
                HoaDon = hoaDon,
                ChiTietHoaDonList = tinh.ChiTiet,
                TongTien = tinh.Tong,
                SoTienSauGiamGia = tinh.SauGiam + tongPhuPhi,
                TongPhuPhi = tongPhuPhi,
                PhuPhiList = phuPhiList
            };

            ViewBag.MaP = maP;
            ViewBag.kh = kh;
            ViewBag.MaGG = maGG;
            ViewBag.TG = hoaDon.Ngaylap;
            ViewBag.listM = view.ChiTietHoaDonList;
            ViewBag.view = view;
            ViewBag.GiaTriGiam = tinh.Giam;
            ViewBag.CaDangMo = await _xuLyKet.LayCaDangMoAsync();
            return View();
        }

        // POST: xác nhận thanh toán -> lưu hóa đơn, xóa phiếu, trả bàn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToan(IFormCollection f)
        {
            string maP = f["MaPh"].ToString();
            string maBan = f["MaBan"].ToString();

            // Tính lại từ giá trong CSDL, không tin số tiền gửi lên từ hidden field
            var tinh = await TinhHoaDonAsync(f["MaMonAn[]"].ToArray()!, f["SoLuong[]"].ToArray()!, f["MaGG"].ToString());
            if (tinh.ChiTiet.Count == 0)
            {
                TempData["ErrorMessage"] = "Hóa đơn không có món nào!";
                return RedirectToAction(nameof(DanhSachPhieuGoiMon));
            }

            // Thanh toán tại quán phải có ca đang mở (tiền mặt được cộng vào két của ca)
            var ca = await _xuLyKet.LayCaDangMoAsync();
            if (ca == null)
            {
                TempData["ErrorMessage"] = "Chưa mở ca làm việc. Vào \"Ca làm việc & két\" để mở ca rồi thanh toán lại.";
                return RedirectToAction(nameof(ChiTietPhieu), new { ma = maP.Trim(), maBan });
            }
            string hinhThucGui = f["HinhThuc"].ToString().Trim();
            if (hinhThucGui != HinhThucThanhToan.TienMat && hinhThucGui != HinhThucThanhToan.ChuyenKhoan)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn hình thức thanh toán: Tiền mặt hoặc Chuyển khoản.";
                return RedirectToAction(nameof(ChiTietPhieu), new { ma = maP.Trim(), maBan });
            }
            bool tienMat = hinhThucGui == HinhThucThanhToan.TienMat;

            // Phụ phí / bồi thường của phiếu (nếu có) - cộng thêm vào số khách phải trả, không bị mã giảm giá áp dụng
            var phuPhiList = string.IsNullOrWhiteSpace(maP) ? new List<PhuPhi>()
                : await _context.PhuPhis.Where(p => p.Mapgm == maP.Trim()).ToListAsync();
            int tongPhuPhi = phuPhiList.Sum(p => p.Sotien);
            int tongPhaiTra = tinh.SauGiam + tongPhuPhi;

            int.TryParse(f["TienKhachDua"].ToString().Replace(".", "").Replace(",", ""), out int tienKhachDua);
            if (tienMat && tienKhachDua > 0 && tienKhachDua < tongPhaiTra)
            {
                TempData["ErrorMessage"] = $"Tiền khách đưa ({tienKhachDua:N0}đ) ít hơn số phải trả ({tongPhaiTra:N0}đ).";
                return RedirectToAction(nameof(ChiTietPhieu), new { ma = maP.Trim(), maBan });
            }

            string maHD = f["MaHD"].ToString().Trim();
            if (maHD == "" || await _context.HoaDons.AnyAsync(h => h.Mahoadon == maHD))
                maHD = await SinhMaHoaDonAsync();

            // Khách của phiếu (phiếu sẽ bị xóa sau khi thanh toán -> chép tên / SĐT sang hóa đơn)
            var phieuTT = string.IsNullOrWhiteSpace(maP) ? null
                : await _context.PhieuGoiMons.AsNoTracking().FirstOrDefaultAsync(p => p.Mapgm == maP.Trim());
            var maKHHoaDon = NullNeuRong(f["MaKH"]) ?? NullNeuRong(phieuTT?.Makh);
            var khTT = maKHHoaDon == null ? null
                : await _context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.Makh == maKHHoaDon);

            var hd = new HoaDon
            {
                Mahoadon = maHD,
                Maban = NullNeuRong(maBan),
                Makh = khTT?.Makh,
                Tenkhach = khTT?.Hoten ?? NullNeuRong(phieuTT?.Tenkhach),
                Sdtkhach = NullNeuRong(khTT?.Sodt) ?? NullNeuRong(phieuTT?.Sdtkhach),
                Manv = NullNeuRong(f["MaNV"]) ?? HttpContext.Session.GetString("MaNV"),
                Magiamgia = tinh.MaHopLe,
                Ngaylap = DateTime.Now,
                Tongtien = tongPhaiTra,   // số tiền thực thu (món sau giảm giá + phụ phí/bồi thường); điểm tích lũy tính trên số này
                Hinhthuc = tienMat ? HinhThucThanhToan.TienMat : HinhThucThanhToan.ChuyenKhoan,
                Loaidon = LoaiDon.TaiQuan,
                Trangthai = TrangThaiDon.HoanThanh,
                Maca = ca.Maca
            };
            foreach (var ct in tinh.ChiTiet)
            {
                ct.Mahoadon = maHD;
                ct.MamonanNavigation = null!;   // chỉ dùng để hiển thị; nếu giữ lại EF sẽ cố INSERT thêm MonAn
            }
            // Gán hóa đơn vừa thanh toán vào các khoản phụ phí của phiếu (để tra cứu sau)
            foreach (var pp in phuPhiList) pp.Mahoadon = maHD;

            await using var tx = await _context.Database.BeginTransactionAsync();

            await _xuLyThanhToan.XuLyHoaDonAsync(tinh.ChiTiet, hd);

            // Tiền mặt -> cộng vào két (số thực thu; tiền thối đã trả lại khách nên không tính)
            // Tách riêng tiền bán hàng (ThuBan) và tiền bồi thường (ThuBoiThuong) để dễ đối soát theo ca
            if (tienMat && tongPhaiTra > 0)
            {
                if (tinh.SauGiam > 0)
                    _xuLyKet.ThemGiaoDich(ca, LoaiGiaoDichKet.ThuBan, tinh.SauGiam,
                        "Thanh toán hóa đơn" + (string.IsNullOrWhiteSpace(maBan) ? "" : " bàn " + maBan.Trim())
                            + (tienKhachDua > tongPhaiTra ? $" (khách đưa {tienKhachDua:N0}, thối {tienKhachDua - tongPhaiTra:N0})" : ""),
                        maHD, hd.Manv);
                if (tongPhuPhi > 0)
                    _xuLyKet.ThemGiaoDich(ca, LoaiGiaoDichKet.ThuBoiThuong, tongPhuPhi,
                        "Thu bồi thường hóa đơn" + (string.IsNullOrWhiteSpace(maBan) ? "" : " bàn " + maBan.Trim())
                            + " (" + string.Join("; ", phuPhiList.Select(p => p.Noidung)) + ")",
                        maHD, hd.Manv);
                await _context.SaveChangesAsync();
            }

            await XoaPhieuVaChiTietAsync(maP);
            await _xuLyMaGiamGia.GiamSoLuongAsync(tinh.MaHopLe);

            var ban = await _context.Bans.FirstOrDefaultAsync(b => b.Maban == maBan);
            if (ban != null)
            {
                // Bàn trống lại + phiếu đặt bàn của bàn này chuyển "Đã phục vụ" (ẩn khỏi danh sách chờ)
                await XuLyBan.GiaiPhongBanAsync(_context, ban);
                await _context.SaveChangesAsync();
            }

            await tx.CommitAsync();

            // Sau khi thanh toán: mở hóa đơn vừa lưu (có nút In hóa đơn)
            TempData["SuccessMessage"] = $"Thanh toán thành công hóa đơn {maHD} ({hd.Hinhthuc})."
                + (tienMat ? $" Đã cộng {tongPhaiTra:N0}đ vào két" + (tongPhuPhi > 0 ? $" (gồm {tongPhuPhi:N0}đ bồi thường)" : "") + "." : "")
                + (tienMat && tienKhachDua > tongPhaiTra ? $" Tiền thối cho khách: {tienKhachDua - tongPhaiTra:N0}đ." : "");
            return RedirectToAction("ShowCTHD", "NhaHang", new { ma = maHD, inNgay = true });
        }

        // ---------------------------------------------------------------- helpers

        private static string? NullNeuRong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private sealed record KetQuaTinh(List<ChiTietHoaDon> ChiTiet, int Tong, int Giam, int SauGiam, string? MaHopLe);

        private async Task<KetQuaTinh> TinhHoaDonAsync(string[] maMon, string[] soLuong, string? maGiamGia)
        {
            var dsMon = await _context.MonAns.AsNoTracking().ToListAsync();
            var chiTiet = new List<ChiTietHoaDon>();

            for (int i = 0; i < maMon.Length && i < soLuong.Length; i++)
            {
                var ma = maMon[i]?.Trim();
                var mon = dsMon.FirstOrDefault(m => m.Mamonan.Trim() == ma);
                if (mon == null || !int.TryParse(soLuong[i], out int sl) || sl <= 0) continue;

                var cu = chiTiet.FirstOrDefault(c => c.Mamonan == mon.Mamonan);
                int gia = (int)(mon.Gia ?? 0);
                if (cu != null)
                {
                    cu.Soluong += sl;
                    cu.Thanhtien = cu.Soluong * gia;
                }
                else
                {
                    chiTiet.Add(new ChiTietHoaDon
                    {
                        Mamonan = mon.Mamonan,
                        Soluong = sl,
                        Thanhtien = sl * gia,
                        MamonanNavigation = mon   // để view lấy Tenmon / Gia
                    });
                }
            }

            int tong = chiTiet.Sum(c => c.Thanhtien ?? 0);
            var ma_gg = await _xuLyMaGiamGia.LayMaHopLeAsync(maGiamGia);
            int giam = ma_gg?.TinhSoTienGiam(tong) ?? 0;   // mã theo % hoặc số tiền cố định
            return new KetQuaTinh(chiTiet, tong, giam, tong - giam, ma_gg?.MaGiamGia_Code);
        }

        private async Task XoaPhieuVaChiTietAsync(string? ma)
        {
            if (string.IsNullOrWhiteSpace(ma)) return;
            ma = ma.Trim();
            await _context.ChiTietGoiMons.Where(c => c.Mapgm == ma).ExecuteDeleteAsync();
            await _context.PhieuGoiMons.Where(p => p.Mapgm == ma).ExecuteDeleteAsync();
        }

        // Bàn có thể mở phiếu gọi món: chưa có phiếu đang mở (bàn trống, hoặc bàn đã xếp khách nhưng chưa gọi món)
        private async Task<List<Ban>> LayBanTrongAsync()
        {
            var dangDung = _context.PhieuGoiMons.Where(p => p.Maban != null).Select(p => p.Maban!);
            var bans = await _context.Bans.AsNoTracking()
                .Include(b => b.MakhNavigation).ThenInclude(k => k!.MataikhoanNavigation)
                .Where(b => !dangDung.Contains(b.Maban))
                .ToListAsync();
            return bans.OrderBy(b => b.Maban.Trim().Length).ThenBy(b => b.Maban.Trim()).ToList();
        }

        private Task DongBoBanAsync() => XuLyBan.DongBoTrangThaiAsync(_context);

        private async Task NapDuLieuChonBanAsync()
        {
            await DongBoBanAsync();
            ViewBag.Ban = await LayBanTrongAsync();
            ViewBag.DsKhach = await _context.KhachHangs.AsNoTracking()
                .Include(k => k.MataikhoanNavigation)
                .OrderBy(k => k.Hoten)
                .ToListAsync();
        }

        // Mã bàn -> bàn (kèm khách đang ngồi) cho trang danh sách phiếu
        private async Task<Dictionary<string, Ban>> LayBanKemKhachAsync()
        {
            var bans = await _context.Bans.AsNoTracking()
                .Include(b => b.MakhNavigation).ThenInclude(k => k!.MataikhoanNavigation)
                .ToListAsync();
            return bans.ToDictionary(b => b.Maban.Trim(), b => b);
        }

        private Task<string> SinhMaPhieuAsync()
            => SinhMaHelper.TaoMaTiepTheoAsync(_context.PhieuGoiMons.Select(p => p.Mapgm), "PGM");

        private Task<string> SinhMaHoaDonAsync()
            => SinhMaHelper.TaoMaTiepTheoAsync(_context.HoaDons.Select(h => h.Mahoadon), "HD");
    }
}
