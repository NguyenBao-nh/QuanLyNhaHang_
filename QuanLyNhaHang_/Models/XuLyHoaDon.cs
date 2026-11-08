using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang_.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyNhaHang_.Services
{
    public class XuLyHoaDon
    {
        private readonly DbQlNhaHangContext _context;

        public XuLyHoaDon(DbQlNhaHangContext context)
        {
            _context = context;
        }

        public async Task<List<PhieuNhapKho>> LayThongTinHoaDonAsync()
        {
            return await _context.PhieuNhapKhos.ToListAsync();
        }

        public async Task<List<ChiTietNhapKho>> LayThongTinChiTietHoaDonAsync(string maNhapKho)
        {
            return await _context.ChiTietNhapKhos
                .Where(c => c.Manhapkho == maNhapKho)
                .ToListAsync();
        }

        public async Task XuLyThemHoaDonAsync(PhieuNhapKho ma)
        {
            _context.PhieuNhapKhos.Add(ma);
            await _context.SaveChangesAsync();
        }

        public async Task XuLyThemChiTietHoaDonAsync(List<ChiTietNhapKho> listCTHD)
        {
            if (listCTHD == null || !listCTHD.Any()) return;

            _context.ChiTietNhapKhos.AddRange(listCTHD);

            // Cập nhật tổng tiền cho phiếu nhập kho
            string maNhapKho = listCTHD[0].Manhapkho;
            var phieuNhap = await _context.PhieuNhapKhos.FindAsync(maNhapKho);
            if (phieuNhap != null)
            {
                int tongTienThem = listCTHD.Sum(x => x.Thanhtien ?? 0);
                phieuNhap.Tongtien = (phieuNhap.Tongtien ?? 0) + tongTienThem;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<ChiTietNhapKhoViewModel>> GetChiTietNhapKhoAsync(string maNhapKho)
        {
            return await _context.ChiTietNhapKhos
                .Where(ctnk => ctnk.Manhapkho == maNhapKho)
                .Join(_context.NguyenLieus,
                      ctnk => ctnk.Manguyenlieu,
                      nl => nl.Manguyenlieu,
                      (ctnk, nl) => new ChiTietNhapKhoViewModel
                      {
                          MaNhapKho = ctnk.Manhapkho,
                          MaNguyenLieu = ctnk.Manguyenlieu,
                          TenNguyenLieu = nl.Tennguyenlieu,
                          SoLuong = ctnk.Soluong ?? 0,
                          ThanhTien = ctnk.Thanhtien ?? 0
                      })
                .ToListAsync();
        }
    }
}