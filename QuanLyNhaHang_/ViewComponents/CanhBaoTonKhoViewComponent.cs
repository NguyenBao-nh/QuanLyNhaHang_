using Microsoft.AspNetCore.Mvc;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

namespace QuanLyNhaHang_.ViewComponents
{
    /// <summary>
    /// Thanh cảnh báo tồn kho thấp ở đầu mọi trang admin (layout_admin):
    /// nguyên liệu có tồn &lt;= mức tối thiểu ("CanhBaoTonKho:MucToiThieu" trong appsettings.json).
    /// Lỗi CSDL thì không hiện gì (không làm sập trang).
    /// </summary>
    public class CanhBaoTonKhoViewComponent : ViewComponent
    {
        private readonly XuLyNguyenLieu _xuLyNguyenLieu;
        private readonly IConfiguration _cfg;

        public CanhBaoTonKhoViewComponent(XuLyNguyenLieu xuLyNguyenLieu, IConfiguration cfg)
        {
            _xuLyNguyenLieu = xuLyNguyenLieu;
            _cfg = cfg;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int muc = XuLyNguyenLieu.MucToiThieu(_cfg);
            List<NguyenLieu> ds;
            try
            {
                ds = await _xuLyNguyenLieu.LayNguyenLieuSapHetAsync(muc);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không kiểm tra được tồn kho: " + ex.Message);
                ds = new List<NguyenLieu>();
            }
            ViewBag.MucToiThieu = muc;
            return View(ds);
        }
    }
}
