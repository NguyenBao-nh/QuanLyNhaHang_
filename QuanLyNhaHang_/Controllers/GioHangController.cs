using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Extensions;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.Controllers
{
    public class GioHangController : Controller
    {
        private const string CartKey = "Cart";

        private readonly DbQlNhaHangContext _context;
        private readonly XuLyMaGiamGia _xuLyMaGiamGia;
        private readonly XuLyThanhToan _xuLyThanhToan;

        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;

        private static readonly string[] _duoiAnh = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long AnhToiDa = 5 * 1024 * 1024; // 5 MB

        public GioHangController(DbQlNhaHangContext context, XuLyMaGiamGia xuLyMaGiamGia, XuLyThanhToan xuLyThanhToan,
                                 IWebHostEnvironment env, IConfiguration cfg)
        {
            _context = context;
            _xuLyMaGiamGia = xuLyMaGiamGia;
            _xuLyThanhToan = xuLyThanhToan;
            _env = env;
            _cfg = cfg;
        }

        // Chống "bom hàng": bắt buộc gửi ảnh chuyển khoản ("DonTrucTuyen:BatBuocAnhChuyenKhoan", mặc định true)
        private bool BatBuocAnhCK => _cfg.GetValue<bool?>("DonTrucTuyen:BatBuocAnhChuyenKhoan") ?? true;

        // Mỗi tài khoản chỉ được có tối đa N đơn đang chờ nhà hàng xác nhận ("DonTrucTuyen:SoDonChoToiDa", mặc định 3)
        private int SoDonChoToiDa => Math.Max(1, _cfg.GetValue<int?>("DonTrucTuyen:SoDonChoToiDa") ?? 3);

        private Task<int> DemDonChoAsync(string maKH) =>
            _context.HoaDons.CountAsync(h => h.Makh == maKH && h.Loaidon == LoaiDon.TrucTuyen && h.Trangthai == TrangThaiDon.ChoXacNhan);

        // Lưu ảnh chuyển khoản vào wwwroot/Images/ChuyenKhoan, trả về tên file (null nếu không hợp lệ)
        private async Task<string?> LuuAnhChuyenKhoanAsync(IFormFile? file, string maHD)
        {
            if (file == null || file.Length == 0 || file.Length > AnhToiDa) return null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_duoiAnh.Contains(ext) || !(file.ContentType?.StartsWith("image/") ?? false)) return null;

            var thuMuc = Path.Combine(_env.WebRootPath, "Images", "ChuyenKhoan");
            Directory.CreateDirectory(thuMuc);
            var ten = $"ck_{maHD.Trim()}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..6]}{ext}";
            await using var fs = new FileStream(Path.Combine(thuMuc, ten), FileMode.Create);
            await file.CopyToAsync(fs);
            return ten;
        }

        private List<GioHang> LayGio() =>
            HttpContext.Session.GetObjectFromJson<List<GioHang>>(CartKey) ?? new List<GioHang>();

        private void LuuGio(List<GioHang> cart) =>
            HttpContext.Session.SetObjectAsJson(CartKey, cart);

        // Bàn mà tài khoản khách đang ngồi (nhân viên xếp ở Quản lý bàn). null = chưa nhận bàn.
        private async Task<string?> LayBanCuaKhachAsync()
        {
            var maKH = HttpContext.Session.GetString("MaKH")?.Trim();
            if (string.IsNullOrEmpty(maKH)) return null;
            var maBan = await _context.Bans.AsNoTracking()
                .Where(b => b.Makh == maKH && b.Trangthai == "Đầy")
                .Select(b => b.Maban)
                .FirstOrDefaultAsync();
            return maBan?.Trim();
        }

        // Giỏ hàng là ngăn trượt nằm trong layout_user (bấm nút "Giỏ hàng"), không có trang riêng.
        // Bản cũ trả View() qua _Layout mặc định nên chỉ hiện trang trắng (vd: bấm Thanh toán khi giỏ trống).
        public IActionResult HienThiGioHang() => RedirectToAction("Menu", "MonAn");

        [HttpPost]
        public async Task<JsonResult> AddToCart(string maMonAn, int soLuong = 1)
        {
            if (soLuong < 1) soLuong = 1;

            // Lấy tên/giá/ảnh từ CSDL thay vì tin giá do trình duyệt gửi lên
            var mon = await _context.MonAns.AsNoTracking().FirstOrDefaultAsync(m => m.Mamonan == maMonAn);
            if (mon == null)
                return Json(new { success = false, message = "Món ăn không tồn tại" });

            var cart = LayGio();
            var item = cart.FirstOrDefault(x => x.MaMonAn == mon.Mamonan.Trim());
            if (item != null)
            {
                item.SoLuong += soLuong;
            }
            else
            {
                cart.Add(new GioHang
                {
                    MaMonAn = mon.Mamonan.Trim(),
                    TenMonAn = mon.Tenmon ?? "",
                    DonGia = mon.Gia ?? 0,
                    AnhMon = mon.AnhMon,
                    SoLuong = soLuong
                });
            }

            LuuGio(cart);
            return Json(new { success = true, cartCount = cart.Count, ban = await LayBanCuaKhachAsync() });
        }

        public IActionResult GetCartItems() => PartialView("CartItemsPartial", LayGio());

        [HttpPost]
        public JsonResult CapNhatSoLuong(string maMonAn, int soLuong)
        {
            var cart = LayGio();
            var item = cart.FirstOrDefault(x => x.MaMonAn == maMonAn);
            if (item != null)
            {
                if (soLuong > 0) item.SoLuong = soLuong;
                else cart.Remove(item);
            }
            LuuGio(cart);
            return Json(new { success = true, tongTien = cart.Sum(x => x.SoLuong * x.DonGia) });
        }

        [HttpPost]
        public JsonResult TangSoLuong(string maMonAn)
        {
            var cart = LayGio();
            var item = cart.FirstOrDefault(x => x.MaMonAn == maMonAn);
            if (item != null) item.SoLuong++;
            LuuGio(cart);
            return TongGio(cart);
        }

        [HttpPost]
        public JsonResult GiamSoLuong(string maMonAn)
        {
            var cart = LayGio();
            var item = cart.FirstOrDefault(x => x.MaMonAn == maMonAn);
            if (item != null && item.SoLuong > 1) item.SoLuong--;
            LuuGio(cart);
            return TongGio(cart);
        }

        [HttpPost]
        public JsonResult RemoveItem(string maMonAn)
        {
            var cart = LayGio();
            var item = cart.FirstOrDefault(x => x.MaMonAn == maMonAn);
            if (item != null) cart.Remove(item);
            LuuGio(cart);
            return TongGio(cart);
        }

        private JsonResult TongGio(List<GioHang> cart) =>
            Json(new
            {
                totalQuantity = cart.Sum(x => x.SoLuong),
                totalPrice = cart.Sum(x => x.SoLuong * x.DonGia)
            });

        // POST: hiển thị hóa đơn + QR chuyển khoản
        [HttpPost]
        public async Task<IActionResult> HienThiHoaDonVaQRThanhToan(IFormCollection f)
        {
            var maKH = HttpContext.Session.GetString("MaKH");
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("MaTaiKhoan")))
                return RedirectToAction("DangNhap", "Account");
            if (string.IsNullOrEmpty(maKH))   // đã đăng nhập nhưng chưa có hồ sơ khách hàng
                return RedirectToAction("NhapThongTinKhachHang", "KhachHang");

            var cart = LayGio();
            if (cart.Count == 0)
            {
                TempData["ThongBao"] = "Giỏ hàng đang trống, hãy chọn món trước khi thanh toán.";
                return RedirectToAction("Menu", "MonAn");
            }

            // Chống bom hàng: còn quá nhiều đơn chưa được nhà hàng xác nhận thanh toán thì chưa cho đặt thêm
            if (await DemDonChoAsync(maKH) >= SoDonChoToiDa)
            {
                TempData["ThongBao"] = $"Bạn đang có {SoDonChoToiDa} đơn chờ nhà hàng xác nhận thanh toán. "
                    + "Vui lòng đợi nhà hàng xác nhận (hoặc liên hệ hotline) rồi đặt tiếp.";
                return RedirectToAction("Profile", "KhachHang");
            }

            int tong = (int)cart.Sum(x => x.SoLuong * x.DonGia);
            var maGG = await _xuLyMaGiamGia.LayMaHopLeAsync(f["MaGiamGia"].ToString());
            int giam = maGG?.TinhSoTienGiam(tong) ?? 0;   // mã theo % hoặc số tiền cố định
            int sauGiam = tong - giam;

            var maBan = await LayBanCuaKhachAsync();
            var hd = new HoaDon
            {
                Mahoadon = await SinhMaHoaDonAsync(),
                Maban = maBan,          // món được gửi về bàn khách đang ngồi
                Makh = maKH,
                Manv = null,
                Magiamgia = maGG?.MaGiamGia_Code,
                Ngaylap = DateTime.Now,
                Tongtien = sauGiam,
                Hinhthuc = "Chuyển khoản"
            };

            var chiTiet = cart.Select(item => new ChiTietHoaDon
            {
                Mahoadon = hd.Mahoadon,
                Mamonan = item.MaMonAn,
                Soluong = item.SoLuong,
                Thanhtien = (int)(item.SoLuong * item.DonGia)
            }).ToList();

            ViewBag.TenKhachHang = HttpContext.Session.GetString("HoTen");
            ViewBag.SoDT = HttpContext.Session.GetString("SoDT")?.Trim();
            ViewBag.BatBuocAnhCK = BatBuocAnhCK;

            // Giỏ hàng chỉ xóa khi thanh toán xong (DaThanhToan), để khách quay lại không bị mất giỏ
            return View(new ViewThanhToan
            {
                HoaDon = hd,
                ChiTietHoaDonList = chiTiet,
                TongTien = tong,
                SoTienSauGiamGia = sauGiam
            });
        }

        // POST: khách xác nhận đã chuyển khoản (gửi kèm ảnh chụp giao dịch).
        // Đơn chỉ được tạo SAU khi khách thanh toán; nhà hàng đối soát tiền rồi mới "Xác nhận đơn".
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(8 * 1024 * 1024)]
        public async Task<IActionResult> DaThanhToan(IFormCollection f)
        {
            var maKHSession = HttpContext.Session.GetString("MaKH");
            if (string.IsNullOrEmpty(maKHSession))
                return RedirectToAction("DangNhap", "Account");

            // Dựng lại chi tiết từ form, nhưng tính tiền theo giá trong CSDL
            var dsMon = await _context.MonAns.AsNoTracking().ToListAsync();
            var chiTiet = new List<ChiTietHoaDon>();
            for (int i = 0; f.ContainsKey($"ChiTietHoaDon[{i}].MaMon"); i++)
            {
                var ma = f[$"ChiTietHoaDon[{i}].MaMon"].ToString().Trim();
                var mon = dsMon.FirstOrDefault(m => m.Mamonan.Trim() == ma);
                if (mon == null || !int.TryParse(f[$"ChiTietHoaDon[{i}].SoLuong"], out int sl) || sl <= 0) continue;

                var cu = chiTiet.FirstOrDefault(c => c.Mamonan == mon.Mamonan);
                if (cu != null) { cu.Soluong += sl; cu.Thanhtien = cu.Soluong * (int)(mon.Gia ?? 0); }
                else chiTiet.Add(new ChiTietHoaDon { Mamonan = mon.Mamonan, Soluong = sl, Thanhtien = sl * (int)(mon.Gia ?? 0) });
            }

            if (chiTiet.Count == 0)
                return RedirectToAction("Menu", "MonAn");

            int tong = chiTiet.Sum(c => c.Thanhtien ?? 0);
            var maGG = await _xuLyMaGiamGia.LayMaHopLeAsync(f["MaGiamGia"].ToString());
            int sauGiam = tong - (maGG?.TinhSoTienGiam(tong) ?? 0);

            // Thông tin nhận món: khách đang ngồi ở bàn -> phục vụ tại bàn; không thì giao tới địa chỉ khách nhập
            var maBanKhach = await LayBanCuaKhachAsync();
            string sdtNhan = f["SdtNhan"].ToString().Trim();
            string diaChi = f["DiaChiGiao"].ToString().Trim();
            string ghiChu = f["GhiChu"].ToString().Trim();
            string maGiaoDich = f["MaGiaoDich"].ToString().Trim();
            var anhCK = f.Files.GetFile("AnhChuyenKhoan");
            string? loiNhan = null;
            if (await DemDonChoAsync(maKHSession) >= SoDonChoToiDa)
                loiNhan = $"Bạn đang có {SoDonChoToiDa} đơn chờ nhà hàng xác nhận thanh toán, vui lòng đợi xác nhận rồi đặt tiếp.";
            else if (BatBuocAnhCK && (anhCK == null || anhCK.Length == 0))
                loiNhan = "Vui lòng chuyển khoản và tải lên ảnh chụp màn hình giao dịch thành công trước khi xác nhận.";
            else if (anhCK != null && anhCK.Length > AnhToiDa)
                loiNhan = "Ảnh chuyển khoản tối đa 5 MB.";
            else if (maGiaoDich.Length > 50)
                loiNhan = "Mã giao dịch tối đa 50 ký tự.";
            else if (sdtNhan == "" || sdtNhan.Length < 9 || sdtNhan.Length > 11 || !sdtNhan.All(char.IsDigit))
                loiNhan = "Vui lòng nhập số điện thoại nhận món hợp lệ (9 - 11 chữ số).";
            else if (maBanKhach == null && diaChi == "")
                loiNhan = "Vui lòng nhập địa chỉ giao hàng.";
            else if (diaChi.Length > 200 || ghiChu.Length > 300)
                loiNhan = "Địa chỉ tối đa 200 ký tự, ghi chú tối đa 300 ký tự.";

            if (loiNhan != null)
            {
                // Hiện lại trang thanh toán (giỏ hàng vẫn còn)
                ViewBag.LoiNhanMon = loiNhan;
                ViewBag.TenKhachHang = HttpContext.Session.GetString("HoTen");
                ViewBag.SoDT = sdtNhan;
                ViewBag.DiaChi = diaChi;
                ViewBag.GhiChu = ghiChu;
                ViewBag.MaGiaoDich = maGiaoDich;
                ViewBag.BatBuocAnhCK = BatBuocAnhCK;
                return View("HienThiHoaDonVaQRThanhToan", new ViewThanhToan
                {
                    HoaDon = new HoaDon
                    {
                        Mahoadon = f["MaHoaDon"].ToString().Trim(),
                        Maban = maBanKhach,
                        Makh = maKHSession,
                        Magiamgia = maGG?.MaGiamGia_Code,
                        Ngaylap = DateTime.Now,
                        Tongtien = sauGiam,
                        Hinhthuc = f["HinhThuc"].ToString()
                    },
                    ChiTietHoaDonList = chiTiet,
                    TongTien = tong,
                    SoTienSauGiamGia = sauGiam
                });
            }

            string maHD = f["MaHoaDon"].ToString().Trim();
            string noiDungCK = maHD;   // nội dung chuyển khoản trên mã QR
            if (maHD == "" || await _context.HoaDons.AnyAsync(h => h.Mahoadon == maHD))
                maHD = await SinhMaHoaDonAsync();

            string? tenAnh = null;
            if (anhCK != null && anhCK.Length > 0)
            {
                tenAnh = await LuuAnhChuyenKhoanAsync(anhCK, maHD);
                if (tenAnh == null)
                {
                    TempData["ThongBao"] = "Ảnh chuyển khoản không hợp lệ (chỉ nhận JPG, PNG, WEBP, tối đa 5 MB). Vui lòng thanh toán lại từ giỏ hàng.";
                    return RedirectToAction("Menu", "MonAn");
                }
            }
            if (noiDungCK != "" && noiDungCK != maHD)
                ghiChu = (ghiChu + $" [Nội dung CK: {noiDungCK}]").Trim();

            var hd = new HoaDon
            {
                Mahoadon = maHD,
                Maban = maBanKhach,            // gắn hóa đơn về bàn của tài khoản này
                Makh = maKHSession,            // lấy từ Session, không tin hidden field
                Manv = null,                   // nhân viên được ghi khi xác nhận đơn
                Magiamgia = maGG?.MaGiamGia_Code,
                Ngaylap = DateTime.Now,
                Tongtien = sauGiam,
                Hinhthuc = f["HinhThuc"].ToString(),
                // Đơn trực tuyến: admin xem ở "Người mua trực tuyến" -> Xác nhận đơn -> Xác nhận giao
                Loaidon = LoaiDon.TrucTuyen,
                Trangthai = TrangThaiDon.ChoXacNhan,
                Tenkhach = HttpContext.Session.GetString("HoTen"),
                Sdtkhach = sdtNhan,
                Diachigiao = maBanKhach != null ? null : diaChi,
                Ghichu = ghiChu == "" ? null : (ghiChu.Length > 300 ? ghiChu[..300] : ghiChu),
                Anhchuyenkhoan = tenAnh,
                Magiaodich = maGiaoDich == "" ? null : maGiaoDich
            };
            foreach (var ct in chiTiet) ct.Mahoadon = maHD;

            await using (var tx = await _context.Database.BeginTransactionAsync())
            {
                // Lưu hóa đơn + chi tiết. Điểm thành viên chỉ cộng khi nhà hàng xác nhận đã nhận đủ tiền.
                await _xuLyThanhToan.XuLyHoaDonAsync(chiTiet, hd, congDiem: false);
                await _xuLyMaGiamGia.GiamSoLuongAsync(hd.Magiamgia);
                await tx.CommitAsync();
            }

            // Đồng bộ điểm trong Session với CSDL
            var diem = await _context.KhachHangs.AsNoTracking()
                .Where(k => k.Makh == maKHSession).Select(k => k.Diemthanhvien).FirstOrDefaultAsync();
            HttpContext.Session.SetInt32("DiemThanhVien", diem ?? 0);

            HttpContext.Session.Remove(CartKey);
            TempData["Cart"] = 0;
            TempData["ThongBao"] = $"Đã gửi xác nhận thanh toán cho đơn {hd.Mahoadon}."
                + " Nhà hàng sẽ kiểm tra tiền chuyển khoản rồi xác nhận đơn"
                + (hd.Maban != null ? $" và phục vụ tại bàn {hd.Maban.Trim()}." : " và giao tới địa chỉ của bạn.")
                + " Bạn theo dõi trạng thái ngay bên dưới.";
            // Mở trang chi tiết hóa đơn vừa lưu (có nút In hóa đơn)
            return RedirectToAction("ShowCTHD", "KhachHang", new { ma = hd.Mahoadon });
        }

        [HttpPost]
        public async Task<JsonResult> KiemTraMa(string discountCode)
            => Json(await _xuLyMaGiamGia.KiemTraAsync(discountCode));

        // POST: GioHang/ThongTinMa -> { hopLe, moTa } (vd "Giảm 10% (tối đa 50.000đ)") để hiện cho khách / nhân viên
        [HttpPost]
        public async Task<JsonResult> ThongTinMa(string discountCode)
        {
            var ma = await _xuLyMaGiamGia.LayMaHopLeAsync(discountCode);
            return Json(new { hopLe = ma != null, moTa = ma?.MoTa });
        }

        private Task<string> SinhMaHoaDonAsync()
            => SinhMaHelper.TaoMaTiepTheoAsync(_context.HoaDons.Select(h => h.Mahoadon), "HD");
    }
}
