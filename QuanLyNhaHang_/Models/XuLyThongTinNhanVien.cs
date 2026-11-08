using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyNhaHang_.Services
{
    public class XuLyThongTinNhanVien
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyThongTinNhanVien(DbQlNhaHangContext context)
        {
            _context = context;
        }

        public async Task<List<NhanVien>> LayThongTinNhanVienAsync()
        {
            return await _context.NhanViens.ToListAsync();
        }
    }
}