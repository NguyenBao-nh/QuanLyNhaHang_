using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.ViewComponents
{
    // Chấm đỏ có số cạnh mục trên thanh menu admin (lớn hơn 99 thì hiện "99+"):
    //   DatBan       - phiếu đặt bàn đang chờ xếp bàn
    //   BanDangDung  - số bàn đang có khách / còn phiếu gọi món
    //   DanhGia      - đánh giá chưa được nhà hàng trả lời
    //   DonTrucTuyen - đơn trực tuyến chờ xác nhận
    // Gọi: @await Component.InvokeAsync("HuyHieuMenu", new { loai = "DatBan" })
    public class HuyHieuMenuViewComponent : ViewComponent
    {
        private readonly DbQlNhaHangContext _context;

        public HuyHieuMenuViewComponent(DbQlNhaHangContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string loai)
        {
            int so = 0;
            string tieuDe = "";
            try
            {
                switch (loai)
                {
                    case "DatBan":
                        so = await _context.PhieuDatBans.CountAsync(p => p.Trangthai == null || p.Trangthai == ""
                                                                      || p.Trangthai == TrangThaiDatBan.ChoXepBan);
                        tieuDe = "phiếu đặt bàn đang chờ xếp bàn";
                        break;
                    case "BanDangDung":
                        so = await _context.Bans.CountAsync(b => b.Trangthai == XuLyBan.Day
                                                              || _context.PhieuGoiMons.Any(p => p.Maban == b.Maban));
                        tieuDe = "bàn đang có khách";
                        break;
                    case "DanhGia":
                        so = await _context.DanhGia.CountAsync(d => d.Phanhoi == null || d.Phanhoi == "");
                        tieuDe = "đánh giá chưa trả lời";
                        break;
                    case "DonTrucTuyen":
                        so = await _context.HoaDons.CountAsync(h => h.Loaidon == LoaiDon.TrucTuyen && h.Trangthai == TrangThaiDon.ChoXacNhan);
                        tieuDe = "đơn trực tuyến chờ xác nhận";
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Không đếm được huy hiệu menu '{loai}': " + ex.Message);
                so = 0;
            }

            if (so <= 0) return Content("");
            return new HtmlContentViewComponentResult(new HtmlString(HtmlHuyHieu(so, tieuDe)));
        }

        // Dùng chung cho các huy hiệu khác (vd SoDonChoXacNhan)
        public static string HtmlHuyHieu(int so, string tieuDe)
        {
            string chu = so > 99 ? "99+" : so.ToString();
            return $"<span class=\"huy-hieu-menu\" title=\"{so} {System.Net.WebUtility.HtmlEncode(tieuDe)}\">{chu}</span>";
        }
    }
}
