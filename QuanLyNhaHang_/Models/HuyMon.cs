using System;

namespace QuanLyNhaHang_.Models;

// Lịch sử hủy món của phiếu gọi món (khách đợi lâu / không muốn dùng nữa).
// Không có khóa ngoại tới PhieuGoiMon vì phiếu bị xóa sau khi thanh toán, lịch sử vẫn giữ.
public partial class HuyMon
{
    public int Mahuy { get; set; }

    public string Mapgm { get; set; } = null!;

    public string? Maban { get; set; }

    public string Mamonan { get; set; } = null!;

    public string? Tenmon { get; set; }

    public int Soluong { get; set; }

    public string? Lydo { get; set; }

    public string? Manv { get; set; }

    public DateTime Thoigian { get; set; }
}

public static class LyDoHuyMon
{
    public static readonly string[] GoiY =
    {
        "Khách đợi quá lâu",
        "Khách không muốn dùng nữa",
        "Bếp hết món / hết nguyên liệu",
        "Gọi nhầm món"
    };
}
