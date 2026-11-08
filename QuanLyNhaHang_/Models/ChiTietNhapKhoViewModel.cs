namespace QuanLyNhaHang_.Models
{
    public class ChiTietNhapKhoViewModel
    {
        public string MaNhapKho { get; set; } = null!;
        public string MaNguyenLieu { get; set; } = null!;
        public string? TenNguyenLieu { get; set; }
        public int SoLuong { get; set; }
        public int ThanhTien { get; set; }
    }
}