using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;

namespace QuanLyNhaHang_.Services
{
    /// <summary>
    /// Dùng chung cho Quản lý bàn / Phiếu gọi món / Khách đặt bàn:
    /// trả bàn về "Trống" và đánh dấu phiếu đặt bàn của bàn đó là "Đã phục vụ"
    /// (phiếu được ẩn khỏi danh sách chờ xếp bàn, không bị xóa).
    /// </summary>
    public static class XuLyBan
    {
        public const string Trong = "Trống";
        public const string Day = "Đầy";

        /// <summary>
        /// Đồng bộ / chuẩn hóa trạng thái bàn. Bàn chỉ có 2 trạng thái: "Trống" hoặc "Đầy".
        /// - Bàn còn phiếu gọi món (chưa thanh toán) -> "Đầy" (VD phiếu B01 đã lên món nhưng bàn vẫn "Trống").
        /// - Trạng thái lạ trong CSDL (VD "Đang phục vụ", "Có khách", viết sai chính tả...):
        ///   còn khách / còn phiếu -> "Đầy", không có gì -> "Trống".
        /// Trả về danh sách mã bàn đã sửa.
        /// </summary>
        public static async Task<List<string>> DongBoTrangThaiAsync(DbQlNhaHangContext context)
        {
            var phieuMo = await context.PhieuGoiMons.AsNoTracking()
                .Where(p => p.Maban != null)
                .ToListAsync();
            var theoBan = phieuMo.GroupBy(p => p.Maban!.Trim())
                                 .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Ngaylap).First());

            var daSua = new List<string>();
            foreach (var b in await context.Bans.ToListAsync())
            {
                var ma = b.Maban.Trim();
                var tt = b.Trangthai?.Trim();
                // Chuẩn Unicode dựng sẵn (NFC): chữ gõ trong SSMS có thể là dạng tổ hợp, nhìn giống "Đầy" nhưng so sánh khác
                var ttChuan = tt?.Normalize();
                bool coPhieu = theoBan.TryGetValue(ma, out var p);
                bool coKhach = b.Makh != null || !string.IsNullOrWhiteSpace(b.Tenkhach);

                string dung;
                if (coPhieu) dung = Day;
                else if (ttChuan == Day || ttChuan == Trong) dung = ttChuan;   // hợp lệ, không có phiếu -> giữ nguyên
                else dung = coKhach ? Day : Trong;                             // trạng thái lạ

                if (b.Trangthai == dung) continue;   // so với giá trị gốc trong CSDL (kể cả khoảng trắng / dạng Unicode)

                b.Trangthai = dung;
                if (dung == Day)
                {
                    b.Giovao ??= p?.Ngaylap ?? DateTime.Now;
                    if (!coKhach && p != null)
                    {
                        b.Makh = p.Makh;
                        b.Tenkhach = p.Tenkhach;
                        b.Sdtkhach = p.Sdtkhach;
                    }
                }
                else
                {
                    b.Giovao = null;
                }
                daSua.Add($"{ma} (\"{tt}\" → \"{dung}\")");
            }
            if (daSua.Count > 0) await context.SaveChangesAsync();
            return daSua;
        }

        /// <summary>Bàn còn phiếu gọi món chưa thanh toán?</summary>
        public static Task<bool> ConPhieuGoiMonAsync(DbQlNhaHangContext context, string maBan)
        {
            maBan = maBan.Trim();
            return context.PhieuGoiMons.AnyAsync(p => p.Maban == maBan);
        }

        /// <param name="ban">Bàn đang được context theo dõi (đọc không dùng AsNoTracking).</param>
        /// <remarks>Chỉ thay đổi dữ liệu, người gọi tự SaveChangesAsync.</remarks>
        public static async Task GiaiPhongBanAsync(DbQlNhaHangContext context, Ban ban)
        {
            var maBan = ban.Maban.Trim();

            ban.Trangthai = Trong;
            ban.Makh = null;
            ban.Tenkhach = null;
            ban.Sdtkhach = null;
            ban.Giovao = null;

            var phieuDat = await context.PhieuDatBans
                .Where(p => p.Maban == maBan && p.Trangthai == TrangThaiDatBan.DangPhucVu)
                .ToListAsync();
            foreach (var p in phieuDat)
                p.Trangthai = TrangThaiDatBan.DaPhucVu;
        }
    }
}
