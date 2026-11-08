using System;

namespace QuanLyNhaHang_.Models;

// Phụ phí / bồi thường ghi vào phiếu gọi món (khách làm vỡ bát, hỏng đồ...).
// Khi xuất hóa đơn: cộng thẳng vào tổng tiền khách phải trả (không bị mã giảm giá áp dụng).
// Khi thanh toán tiền mặt: ghi thêm 1 giao dịch két riêng (LoaiGiaoDichKet.ThuBoiThuong) để tách khỏi tiền bán hàng.
// Không có khóa ngoại tới PhieuGoiMon vì phiếu bị xóa sau khi thanh toán, lịch sử vẫn giữ (Mahoadon được gán sau khi thanh toán).
public partial class PhuPhi
{
    public int Maphuphi { get; set; }

    public string Mapgm { get; set; } = null!;

    public string? Maban { get; set; }

    public string Noidung { get; set; } = null!;

    public int Sotien { get; set; }

    public string? Manv { get; set; }

    public DateTime Thoigian { get; set; }

    public string? Mahoadon { get; set; }
}
