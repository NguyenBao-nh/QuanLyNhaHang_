using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;

namespace QuanLyNhaHang_.ViewComponents
{
    /// <summary>
    /// Số đơn trực tuyến đang chờ xác nhận (huy hiệu đỏ cạnh mục "Người mua trực tuyến" trên sidebar admin).
    /// Lỗi CSDL (chưa chạy script nâng cấp) thì không hiện gì.
    /// </summary>
    public class SoDonChoXacNhanViewComponent : ViewComponent
    {
        private readonly DbQlNhaHangContext _context;

        public SoDonChoXacNhanViewComponent(DbQlNhaHangContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int so = 0;
            try
            {
                so = await _context.HoaDons.CountAsync(h => h.Loaidon == LoaiDon.TrucTuyen && h.Trangthai == TrangThaiDon.ChoXacNhan);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không đếm được đơn trực tuyến: " + ex.Message);
            }
            if (so <= 0) return Content("");
            return new HtmlContentViewComponentResult(new Microsoft.AspNetCore.Html.HtmlString(
                HuyHieuMenuViewComponent.HtmlHuyHieu(so, "đơn trực tuyến chờ xác nhận")));
        }
    }
}
