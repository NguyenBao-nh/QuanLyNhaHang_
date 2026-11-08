using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyNhaHang_.Services
{
    public class XuLyThanhToan
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyThanhToan(DbQlNhaHangContext context)
        {
            _context = context;
        }

        /// <param name="congDiem">false: chưa cộng điểm (đơn trực tuyến chỉ cộng khi nhà hàng xác nhận đã nhận tiền)</param>
        public async Task XuLyHoaDonAsync(List<ChiTietHoaDon> chiTietHoaDonList, HoaDon hd, bool congDiem = true)
        {
            _context.HoaDons.Add(hd);

            if (congDiem && !string.IsNullOrEmpty(hd.Makh))
            {
                var khachHang = await _context.KhachHangs.FindAsync(hd.Makh);
                if (khachHang != null)
                {
                    int tichDiemThem = (int)((hd.Tongtien ?? 0) * 0.01);
                    khachHang.Diemthanhvien = (khachHang.Diemthanhvien ?? 0) + tichDiemThem;
                }
            }

            if (chiTietHoaDonList != null && chiTietHoaDonList.Any())
            {
                _context.ChiTietHoaDons.AddRange(chiTietHoaDonList);
            }

            await _context.SaveChangesAsync();
        }
    }
}