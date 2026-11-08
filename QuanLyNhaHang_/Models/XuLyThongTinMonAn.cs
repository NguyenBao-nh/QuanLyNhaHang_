using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyNhaHang_.Services
{
    public class XuLyThongTinMonAn
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyThongTinMonAn(DbQlNhaHangContext context)
        {
            _context = context;
        }

        public async Task<List<MonAn>> LayThongTinMonAnAsync()
        {
            return await _context.MonAns.ToListAsync();
        }

        public async Task ThemMonAnAsync(MonAn monAn)
        {
            _context.MonAns.Add(monAn);
            await _context.SaveChangesAsync();
        }

        public async Task XoaMonAnAsync(string maMon)
        {
            var monAn = await _context.MonAns.FindAsync(maMon);
            if (monAn != null)
            {
                _context.MonAns.Remove(monAn);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<MonAn?> LayTheoMaAsync(string ma)
        {
            return await _context.MonAns.FindAsync(ma);
        }
    }
}