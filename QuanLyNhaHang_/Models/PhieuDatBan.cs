using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class PhieuDatBan
{
    public string Maphieu { get; set; } = null!;

    public string? Makh { get; set; }

    public DateTime? Ngaydat { get; set; }

    public string? Tenkh { get; set; }

    public int? Soluong { get; set; }

    public string? Email { get; set; }

    public string? Sdt { get; set; }

    public string? Giodat { get; set; }

    // Trạng thái phiếu: null / "Chờ xếp bàn" -> "Đang phục vụ" -> "Đã phục vụ" (hoặc "Đã hủy")
    // Phiếu "Đã phục vụ" / "Đã hủy" được ẩn khỏi danh sách chờ (không xóa, xem lại được ở tab tương ứng)
    public string? Trangthai { get; set; }

    // Bàn đã xếp cho phiếu này
    public string? Maban { get; set; }
}

public static class TrangThaiDatBan
{
    public const string ChoXepBan = "Chờ xếp bàn";
    public const string DangPhucVu = "Đang phục vụ";
    public const string DaPhucVu = "Đã phục vụ";
    public const string DaHuy = "Đã hủy";

    // null (phiếu cũ trước khi có cột TRANGTHAI) cũng tính là đang chờ
    public static bool LaChoXepBan(string? tt) => string.IsNullOrWhiteSpace(tt) || tt.Trim() == ChoXepBan;
}
