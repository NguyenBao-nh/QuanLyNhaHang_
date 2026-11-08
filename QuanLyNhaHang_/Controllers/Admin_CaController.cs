using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    [KhuVucNhanVien]
    [AutoValidateAntiforgeryToken]
    public class Admin_CaController : Controller
    {
        private readonly DbQlNhaHangContext _context;
        private readonly XuLyKet _xuLyKet;

        public Admin_CaController(DbQlNhaHangContext context, XuLyKet xuLyKet)
        {
            _context = context;
            _xuLyKet = xuLyKet;
        }

        private string? MaNV => HttpContext.Session.GetString("MaNV")?.Trim();

        private async Task<string?> TenNVAsync(string? maNV)
        {
            if (string.IsNullOrWhiteSpace(maNV)) return HttpContext.Session.GetString("HoTen");
            return await _context.NhanViens.AsNoTracking().Where(n => n.Manv == maNV)
                       .Select(n => n.Hoten).FirstOrDefaultAsync()
                   ?? HttpContext.Session.GetString("HoTen");
        }

        // Sau khi ghi thu / chi: quay lại trang đã bấm (Quỹ hoặc Ca làm việc). Hai trang dùng chung dữ liệu giao dịch két.
        private IActionResult VeTrang(string? veTrang)
            => veTrang == "Quy" ? RedirectToAction("Index", "Admin_Quy") : RedirectToAction(nameof(Index));

        // Admin_Ca/Index -> ca đang mở (két, giao dịch) hoặc form mở ca
        public async Task<IActionResult> Index()
        {
            CaLamViec? ca;
            try
            {
                ca = await _context.CaLamViecs.AsNoTracking()
                    .Where(c => c.Trangthai == TrangThaiCa.DangMo)
                    .OrderByDescending(c => c.Tgmo).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                TempData["LoiCa"] = "Chưa đọc được ca làm việc (đã chạy Database/NangCap_Dot9_CaLamViec_Ket.sql chưa?): " + ex.Message;
                return View(new TrangCaViewModel());
            }

            var vm = new TrangCaViewModel { CaDangMo = ca };
            if (ca != null)
            {
                vm.TongKet = await _xuLyKet.TinhAsync(ca);
                vm.GiaoDich = await _context.GiaoDichKets.AsNoTracking()
                    .Where(g => g.Maca == ca.Maca)
                    .OrderByDescending(g => g.Thoigian).ThenByDescending(g => g.Magd)
                    .ToListAsync();
            }
            else
            {
                // Gợi ý tiền đầu ca = tiền két cuối của ca trước
                var caTruoc = await _context.CaLamViecs.AsNoTracking()
                    .Where(c => c.Trangthai == TrangThaiCa.DaDong)
                    .OrderByDescending(c => c.Tgdong).FirstOrDefaultAsync();
                vm.CaTruoc = caTruoc;
                vm.GoiYTienDauCa = caTruoc?.Tienthucte ?? caTruoc?.Tienketcuoi ?? 0;
                vm.GoiYLoaiCa = LoaiCa.TheoGio(DateTime.Now);
                vm.CaDaMoHomNay = await _context.CaLamViecs.AsNoTracking()
                    .Where(c => c.Ngay == DateTime.Today).Select(c => c.Loaica).ToListAsync();
            }
            return View(vm);
        }

        // POST: mở ca
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoCa(string? LoaiCaChon, int? TienDauCa)
        {
            if (await _xuLyKet.LayCaDangMoAsync() is CaLamViec dangMo)
            {
                TempData["LoiCa"] = $"Ca {dangMo.Loaica} ({dangMo.Maca.Trim()}) đang mở. Hãy đóng ca đó trước khi mở ca mới.";
                return RedirectToAction(nameof(Index));
            }
            if (LoaiCaChon == null || !LoaiCa.TatCa.Contains(LoaiCaChon))
            {
                TempData["LoiCa"] = "Vui lòng chọn ca: Sáng, Chiều hoặc Tối.";
                return RedirectToAction(nameof(Index));
            }
            if (TienDauCa == null || TienDauCa < 0)
            {
                TempData["LoiCa"] = "Tiền đầu ca phải là số từ 0 trở lên (đếm tiền đang có trong két).";
                return RedirectToAction(nameof(Index));
            }
            var homNay = DateTime.Today;
            if (await _context.CaLamViecs.AnyAsync(c => c.Ngay == homNay && c.Loaica == LoaiCaChon))
            {
                TempData["LoiCa"] = $"Hôm nay đã có ca {LoaiCaChon}. Chọn ca khác.";
                return RedirectToAction(nameof(Index));
            }

            var ca = new CaLamViec
            {
                Maca = await SinhMaHelper.TaoMaTiepTheoAsync(_context.CaLamViecs.Select(c => c.Maca), "CA"),
                Loaica = LoaiCaChon,
                Ngay = homNay,
                Trangthai = TrangThaiCa.DangMo,
                Manvmo = MaNV,
                Tennvmo = await TenNVAsync(MaNV),
                Tgmo = DateTime.Now,
                Tiendauca = TienDauCa.Value
            };
            _context.CaLamViecs.Add(ca);
            await _context.SaveChangesAsync();

            TempData["ThanhCongCa"] = $"Đã mở ca {ca.Loaica} ({ca.Maca}) với tiền đầu ca {ca.Tiendauca:N0}đ.";
            return RedirectToAction(nameof(Index));
        }

        // POST: chi tiêu vặt / rút tiền khỏi két (két đầy). Loai = ChiTieu | RutTien
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GhiTienRa(string? Loai, int? SoTien, string? NoiDung, string? veTrang = null)
        {
            var ca = await _xuLyKet.LayCaDangMoAsync();
            if (ca == null)
            {
                TempData["LoiCa"] = "Chưa mở ca - không thể chi / rút tiền.";
                return VeTrang(veTrang);
            }
            if (Loai != LoaiGiaoDichKet.ChiTieu && Loai != LoaiGiaoDichKet.RutTien)
            {
                TempData["LoiCa"] = "Loại giao dịch không hợp lệ.";
                return VeTrang(veTrang);
            }
            NoiDung = NoiDung?.Trim();
            if (SoTien == null || SoTien <= 0)
                TempData["LoiCa"] = "Số tiền phải lớn hơn 0.";
            else if (Loai == LoaiGiaoDichKet.ChiTieu && string.IsNullOrEmpty(NoiDung))
                TempData["LoiCa"] = "Vui lòng ghi chi cho việc gì (VD: mua đá, gas, rau thêm...).";
            else
            {
                var tienKet = (await _xuLyKet.TinhAsync(ca)).TienKet;
                if (SoTien > tienKet)
                    TempData["LoiCa"] = $"Tiền trong két chỉ còn {tienKet:N0}đ, không đủ để {(Loai == LoaiGiaoDichKet.RutTien ? "rút" : "chi")} {SoTien:N0}đ.";
                else
                {
                    _xuLyKet.ThemGiaoDich(ca, Loai, SoTien.Value,
                        string.IsNullOrEmpty(NoiDung) ? (Loai == LoaiGiaoDichKet.RutTien ? "Két đầy - rút tiền" : null) : NoiDung,
                        null, MaNV);
                    await _context.SaveChangesAsync();
                    TempData["ThanhCongCa"] = $"Đã ghi {LoaiGiaoDichKet.Ten(Loai).ToLower()} {SoTien:N0}đ. Tiền két còn {(tienKet - SoTien.Value):N0}đ.";
                }
            }
            return VeTrang(veTrang);
        }

        // POST: thu bồi thường không gắn với hóa đơn nào (VD: khách đã về mới phát hiện làm vỡ bát, hỏng đồ).
        // Khác với nút "Thêm phụ phí" ở Chi tiết phiếu gọi món (gắn vào hóa đơn của bàn đang phục vụ).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThuBoiThuong(int? SoTien, string? NoiDung, string? veTrang = null)
        {
            var ca = await _xuLyKet.LayCaDangMoAsync();
            if (ca == null)
            {
                TempData["LoiCa"] = "Chưa mở ca - không thể ghi thu bồi thường.";
                return VeTrang(veTrang);
            }
            NoiDung = NoiDung?.Trim();
            if (SoTien == null || SoTien <= 0)
                TempData["LoiCa"] = "Số tiền bồi thường phải lớn hơn 0.";
            else if (string.IsNullOrEmpty(NoiDung))
                TempData["LoiCa"] = "Vui lòng ghi lý do thu bồi thường (VD: khách làm vỡ 2 bát).";
            else
            {
                _xuLyKet.ThemGiaoDich(ca, LoaiGiaoDichKet.ThuBoiThuong, SoTien.Value, NoiDung, null, MaNV);
                await _context.SaveChangesAsync();
                TempData["ThanhCongCa"] = $"Đã ghi thu bồi thường {SoTien.Value:N0}đ ({NoiDung}). Đã cộng vào két.";
            }
            return VeTrang(veTrang);
        }

        // POST: xóa giao dịch ghi nhầm (chỉ trong ca đang mở; giao dịch thu tiền bán hàng / nhập kho / bồi thường gắn hóa đơn không xóa ở đây)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaGiaoDich(int id, string? veTrang = null)
        {
            var ca = await _xuLyKet.LayCaDangMoAsync();
            var gd = await _context.GiaoDichKets.FindAsync(id);
            // Khoản bồi thường chỉ xóa được nếu ghi riêng (không gắn hóa đơn nào - không có mã chứng từ)
            bool xoaDuoc = gd != null && (gd.Loai.Trim() == LoaiGiaoDichKet.ChiTieu || gd.Loai.Trim() == LoaiGiaoDichKet.RutTien
                || (gd.Loai.Trim() == LoaiGiaoDichKet.ThuBoiThuong && string.IsNullOrWhiteSpace(gd.Machungtu)));
            if (ca == null || gd == null || gd.Maca != ca.Maca || !xoaDuoc)
            {
                TempData["LoiCa"] = "Chỉ xóa được khoản chi tiêu / rút tiền / bồi thường ghi riêng (không gắn hóa đơn) của ca đang mở.";
                return VeTrang(veTrang);
            }
            _context.GiaoDichKets.Remove(gd);
            await _context.SaveChangesAsync();
            TempData["ThanhCongCa"] = $"Đã xóa khoản {LoaiGiaoDichKet.Ten(gd.Loai).ToLower()} {gd.Sotien:N0}đ.";
            return VeTrang(veTrang);
        }

        // POST: đóng ca -> chốt số liệu, lưu phiếu đóng ca, mở trang in
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DongCa(int? TienThucTe, string? GhiChu)
        {
            var ca = await _xuLyKet.LayCaDangMoAsync();
            if (ca == null)
            {
                TempData["LoiCa"] = "Không có ca nào đang mở.";
                return RedirectToAction(nameof(Index));
            }
            if (TienThucTe != null && TienThucTe < 0)
            {
                TempData["LoiCa"] = "Tiền đếm thực tế không được âm.";
                return RedirectToAction(nameof(Index));
            }

            var tk = await _xuLyKet.TinhAsync(ca);
            ca.Trangthai = TrangThaiCa.DaDong;
            ca.Manvdong = MaNV;
            ca.Tennvdong = await TenNVAsync(MaNV);
            ca.Tgdong = DateTime.Now;
            ca.Sohoadon = tk.SoHoaDon;
            ca.Doanhthu = tk.DoanhThu;
            ca.Thutienmat = tk.ThuTienMat;
            ca.Thuchuyenkhoan = tk.DoanhThuChuyenKhoan;
            ca.Tongchi = tk.TongChi;
            ca.Tongrut = tk.TienRut;
            ca.Tienketcuoi = tk.TienKet;
            ca.Tienthucte = TienThucTe;
            GhiChu = GhiChu?.Trim();
            ca.Ghichu = string.IsNullOrEmpty(GhiChu) ? null : (GhiChu.Length > 300 ? GhiChu[..300] : GhiChu);
            await _context.SaveChangesAsync();

            TempData["ThanhCongCa"] = $"Đã đóng ca {ca.Loaica} ({ca.Maca.Trim()}). Phiếu đóng ca đã được lưu.";
            return RedirectToAction(nameof(PhieuDongCa), new { ma = ca.Maca.Trim(), auto = 1 });
        }

        // POST: mở lại ca vừa đóng (đóng nhầm / cần nhập thêm chi phí - thu chi ngoài ca).
        // Chỉ mở lại được ca đã đóng gần nhất, và chỉ khi không có ca nào khác đang mở.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoLaiCa(string ma)
        {
            ma = ma?.Trim() ?? "";
            if (await _xuLyKet.LayCaDangMoAsync() is CaLamViec dangMo)
            {
                TempData["LoiCa"] = $"Ca {dangMo.Loaica} ({dangMo.Maca.Trim()}) đang mở. Hãy đóng ca đó trước khi mở lại ca khác.";
                return RedirectToAction(nameof(Index));
            }

            var caDongGanNhat = await _context.CaLamViecs
                .Where(c => c.Trangthai == TrangThaiCa.DaDong)
                .OrderByDescending(c => c.Tgdong).FirstOrDefaultAsync();
            if (caDongGanNhat == null || caDongGanNhat.Maca.Trim() != ma)
            {
                TempData["LoiCa"] = "Chỉ mở lại được ca đã đóng gần nhất (ca cũ hơn không mở lại được vì đã có ca mới hơn).";
                return RedirectToAction(nameof(Index));
            }

            caDongGanNhat.Trangthai = TrangThaiCa.DangMo;
            caDongGanNhat.Manvdong = null;
            caDongGanNhat.Tennvdong = null;
            caDongGanNhat.Tgdong = null;
            caDongGanNhat.Sohoadon = null;
            caDongGanNhat.Doanhthu = null;
            caDongGanNhat.Thutienmat = null;
            caDongGanNhat.Thuchuyenkhoan = null;
            caDongGanNhat.Tongchi = null;
            caDongGanNhat.Tongrut = null;
            caDongGanNhat.Tienketcuoi = null;
            caDongGanNhat.Tienthucte = null;
            await _context.SaveChangesAsync();

            TempData["ThanhCongCa"] = $"Đã mở lại ca {caDongGanNhat.Loaica} ({caDongGanNhat.Maca.Trim()}). Nhập xong chi phí/thu chi thì đóng ca lại.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> PhieuDongCa(string ma)
        {
            ma = ma?.Trim() ?? "";
            var ca = await _context.CaLamViecs.AsNoTracking().FirstOrDefaultAsync(c => c.Maca == ma);
            if (ca == null)
            {
                TempData["LoiCa"] = "Không tìm thấy ca.";
                return RedirectToAction(nameof(LichSu));
            }
            ViewBag.GiaoDich = await _context.GiaoDichKets.AsNoTracking()
                .Where(g => g.Maca == ma && g.Loai != LoaiGiaoDichKet.ThuBan)
                .OrderBy(g => g.Thoigian).ToListAsync();
            // Ca chưa đóng thì tính tạm
            ViewBag.TongKet = ca.DangMo ? await _xuLyKet.TinhAsync(ca) : null;
            return View(ca);
        }

        public async Task<IActionResult> LichSu(int? thang = null, int? nam = null)
        {
            var homNay = DateTime.Today;
            int n = nam is >= 2000 and <= 2100 ? nam.Value : homNay.Year;
            int t = thang is >= 1 and <= 12 ? thang.Value : homNay.Month;
            var tu = new DateTime(n, t, 1);
            var den = tu.AddMonths(1);

            var ds = await _context.CaLamViecs.AsNoTracking()
                .Where(c => c.Ngay >= tu && c.Ngay < den)
                .OrderByDescending(c => c.Tgmo)
                .ToListAsync();
            ViewBag.Thang = t;
            ViewBag.Nam = n;
            return View(ds);
        }
    }
}
