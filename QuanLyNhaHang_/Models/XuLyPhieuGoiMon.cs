using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyNhaHang_.Services
{
    public class XuLyPhieuGoiMon
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyPhieuGoiMon(DbQlNhaHangContext context)
        {
            _context = context;
        }

        public async Task<List<PhieuGoiMon>> LayThongTinPhieuGoiMonAsync()
        {
            return await _context.PhieuGoiMons.AsNoTracking()
                .Include(p => p.MakhNavigation).ThenInclude(k => k!.MataikhoanNavigation)
                .OrderByDescending(p => p.Ngaylap)
                .ToListAsync();
        }

        public async Task<List<ChiTietPhieuGoiMonViewModel>> GetChiTietPhieuGoiMonAsync(string maPgm)
        {
            return await (from ct in _context.ChiTietGoiMons
                          join m in _context.MonAns on ct.Mamonan equals m.Mamonan
                          where ct.Mapgm == maPgm
                          select new ChiTietPhieuGoiMonViewModel
                          {
                              MaMonAn = m.Mamonan,
                              TenMon = m.Tenmon,
                              SoLuong = ct.Soluong ?? 0,
                              DonGia = (int)(m.Gia ?? 0),
                              ThanhTien = (ct.Soluong ?? 0) * (int)(m.Gia ?? 0)
                          }).ToListAsync();
        }
    }
}