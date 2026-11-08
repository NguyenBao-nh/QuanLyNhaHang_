using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyNhaHang_.Services
{
    public class XuLyThongTinKhachHang
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyThongTinKhachHang(DbQlNhaHangContext context)
        {
            _context = context;
        }

        public async Task<List<KhachHang>> LayThongTinKhachHangAsync()
        {
            return await _context.KhachHangs.ToListAsync();
        }

        public async Task<KhachHang?> LayThongTinTuSDTAsync(string sdt)
        {
            return await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.Sodt == sdt);
        }

        public async Task<List<HoaDon>> HoaDonTheoMaKhachAsync(string ma)
        {
            return await _context.HoaDons
                .Where(h => h.Makh == ma)
                .ToListAsync();
        }
    }
}