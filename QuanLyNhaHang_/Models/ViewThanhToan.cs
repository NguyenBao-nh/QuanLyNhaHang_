namespace QuanLyNhaHang_.Models
{
    public class ViewThanhToan
    {
        // Thông tin chung của Hóa đơn (Chứa mã hóa đơn, mã KH, ngày lập, hình thức,...)
        public HoaDon HoaDon { get; set; } = null!;

        // Danh sách các chi tiết món ăn trong hóa đơn
        public List<ChiTietHoaDon> ChiTietHoaDonList { get; set; } = new List<ChiTietHoaDon>();

        // Tổng tiền ban đầu của đơn hàng (trước khi giảm giá)
        public double TongTien { get; set; }

        // Số tiền thực tế khách phải trả (sau khi áp dụng mã giảm giá, đã gồm phụ phí/bồi thường)
        public int SoTienSauGiamGia { get; set; }

        // Phụ phí / bồi thường (khách làm vỡ bát, hỏng đồ...) - không bị mã giảm giá áp dụng
        public int TongPhuPhi { get; set; }
        public List<PhuPhi> PhuPhiList { get; set; } = new List<PhuPhi>();
    }
}