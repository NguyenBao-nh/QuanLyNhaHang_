using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace QuanLyNhaHang_.Services
{
    public class XuLyDatBan
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyDatBan(DbQlNhaHangContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Danh sách phiếu đặt bàn cho trang admin.
        /// tab: "cho" (mặc định) = chờ xếp bàn | "dang" = đang phục vụ | "xong" = đã phục vụ / đã hủy | "tatca".
        /// (Bản cũ chỉ lấy phiếu từ hôm nay đến 2 ngày sau nên khách đặt xa hơn không hiện trong admin.)
        /// </summary>
        public async Task<List<PhieuDatBan>> LayThongTinPhieuDatBanAsync(string? tab = null)
        {
            var q = _context.PhieuDatBans.AsNoTracking();
            switch (tab)
            {
                case "dang":
                    q = q.Where(p => p.Trangthai == TrangThaiDatBan.DangPhucVu);
                    break;
                case "xong":
                    q = q.Where(p => p.Trangthai == TrangThaiDatBan.DaPhucVu || p.Trangthai == TrangThaiDatBan.DaHuy);
                    return await q.OrderByDescending(p => p.Ngaydat).ThenByDescending(p => p.Giodat).ToListAsync();
                case "tatca":
                    return await q.OrderByDescending(p => p.Ngaydat).ThenByDescending(p => p.Giodat).ToListAsync();
                default:
                    q = q.Where(p => p.Trangthai == null || p.Trangthai == "" || p.Trangthai == TrangThaiDatBan.ChoXepBan);
                    break;
            }
            return await q.OrderBy(p => p.Ngaydat).ThenBy(p => p.Giodat).ToListAsync();
        }

        // Số phiếu theo từng tab (hiện trên các nút lọc)
        public async Task<Dictionary<string, int>> DemTheoTrangThaiAsync()
        {
            var ds = await _context.PhieuDatBans.AsNoTracking().Select(p => p.Trangthai).ToListAsync();
            return new Dictionary<string, int>
            {
                ["cho"] = ds.Count(TrangThaiDatBan.LaChoXepBan),
                ["dang"] = ds.Count(t => t == TrangThaiDatBan.DangPhucVu),
                ["xong"] = ds.Count(t => t == TrangThaiDatBan.DaPhucVu || t == TrangThaiDatBan.DaHuy),
                ["tatca"] = ds.Count
            };
        }

        // Gửi mail xác nhận đặt bàn. Không ném lỗi ra ngoài (chỉ ghi log) để không làm hỏng việc đặt bàn.
        public async Task GuiMailAsync(string? email, string? tenKH, DateTime? ngayDat, string? sdt, int? soLuong, string? gioDat)
        {
            if (string.IsNullOrWhiteSpace(email)) return;

            const string taiKhoanGui = "chaytue0203@gmail.com";
            const string matKhauUngDung = "kctw ltds teaj luvb";

            string E(string? x) => System.Net.WebUtility.HtmlEncode(x ?? "");

            try
            {
                using var mail = new MailMessage
                {
                    From = new MailAddress(taiKhoanGui),
                    Subject = "Xác nhận thông tin đặt bàn",
                    IsBodyHtml = true,
                    Body = $@"
    <h3>Kính gửi: {E(tenKH)},</h3>
    <p>Chúng tôi xin xác nhận thông tin đặt bàn của bạn như sau:</p>
    <ul>
        <li><strong>Tên khách hàng: </strong>{E(tenKH)}</li>
        <li><strong>Số điện thoại: </strong>{E(sdt)}</li>
        <li><strong>Số lượng người: </strong>{soLuong} người</li>
        <li><strong>Ngày đặt: </strong>{ngayDat:dd/MM/yyyy}</li>
        <li><strong>Giờ đặt: </strong>{E(gioDat)}</li>
    </ul>
    <p>Vui lòng đến đúng giờ để tránh ảnh hưởng đến trải nghiệm của bạn.</p>"
                };
                mail.To.Add(email.Trim());

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(taiKhoanGui, matKhauUngDung),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi gửi email: " + ex.Message);
            }
        }
    }
}