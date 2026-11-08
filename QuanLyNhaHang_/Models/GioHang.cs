namespace QuanLyNhaHang_.Models
{
    public class GioHang
    {
        public string MaMonAn { get; set; } = string.Empty;
        public string TenMonAn { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public double DonGia { get; set; }
        public string? AnhMon { get; set; }
        public double ThanhTien => SoLuong * DonGia; // Tính thành tiền tự động
    }
}