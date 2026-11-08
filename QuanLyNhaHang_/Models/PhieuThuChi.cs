using System;

namespace QuanLyNhaHang_.Models;

// Phiếu thu / chi khác của quỹ (không phải bán hàng hay nhập kho): vốn góp, điện nước, lương, mặt bằng...
public partial class PhieuThuChi
{
    public string Maphieu { get; set; } = null!;

    // "Thu" | "Chi"
    public string Loai { get; set; } = null!;

    public string? Danhmuc { get; set; }

    public int Sotien { get; set; }

    public string? Noidung { get; set; }

    public DateTime Ngay { get; set; }

    public string? Manv { get; set; }
}

public static class LoaiThuChi
{
    public const string Thu = "Thu";
    public const string Chi = "Chi";

    public static readonly string[] DanhMucThu = { "Vốn góp", "Thu khác" };
    public static readonly string[] DanhMucChi = { "Lương nhân viên", "Điện nước", "Mặt bằng", "Sửa chữa - thiết bị", "Hoàn tiền khách", "Chi khác" };
}
