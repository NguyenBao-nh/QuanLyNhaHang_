using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyNhaHang_.Services
{
    public class XuLyGiamGia
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyGiamGia(DbQlNhaHangContext context)
        {
            _context = context;
        }

        public async Task<bool> KiemTraMaGiamGiaAsync(string maGiamGia)
        {
            var now = DateTime.Now;
            var count = await _context.GiamGia
                .CountAsync(g => g.Magiamgia == maGiamGia
                              && g.Soluong > 0
                              && g.Ngaybd <= now
                              && g.Ngaykt >= now);

            return count > 0;
        }

        public async Task GiamSoLuongMaKhiThanhToanAsync(string ma)
        {
            var giamGia = await _context.GiamGia.FindAsync(ma);
            if (giamGia != null && giamGia.Soluong.HasValue && giamGia.Soluong > 0)
            {
                giamGia.Soluong -= 1;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ThemMaGiamGiaAsync(string ma, DateTime ngayBD, DateTime ngayKT, int soLuong)
        {
            var newGiamGia = new GiamGia
            {
                Magiamgia = ma,
                Ngaybd = ngayBD.Date,
                Ngaykt = ngayKT.Date,
                Soluong = soLuong
            };

            _context.GiamGia.Add(newGiamGia);
            await _context.SaveChangesAsync();
        }
    }
}