using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class DanhGia
{
    public string Madanhgia { get; set; } = null!;

    // Hóa đơn liên quan (không bắt buộc - giữ lại từ bảng cũ)
    public string? Mahoadon { get; set; }

    // Nội dung đánh giá
    public string? Danhgia { get; set; }

    // Tài khoản viết đánh giá (bắt buộc đăng nhập mới được đánh giá)
    public string? Mataikhoan { get; set; }

    // Đối tượng đánh giá: "NhaHang" | "MonAn" | "NhanVien"
    public string? Loaidanhgia { get; set; }

    // Món ăn được đánh giá (khi Loaidanhgia = MonAn)
    public string? Mamonan { get; set; }

    // Nhân viên được đánh giá (khi Loaidanhgia = NhanVien)
    public string? Manv { get; set; }

    // Số sao 1 - 5
    public int? Sosao { get; set; }

    // Tên file ảnh trong wwwroot/Images/DanhGia (không bắt buộc)
    public string? Hinhanh { get; set; }

    public DateTime? Ngaydanhgia { get; set; }

    // Nhà hàng (admin / nhân viên) trả lời đánh giá
    public string? Phanhoi { get; set; }

    public DateTime? Ngayphanhoi { get; set; }

    public string? Manvphanhoi { get; set; }

    public virtual HoaDon? MahoadonNavigation { get; set; }

    public virtual TaiKhoan? MataikhoanNavigation { get; set; }

    public virtual MonAn? MamonanNavigation { get; set; }

    public virtual NhanVien? ManvNavigation { get; set; }
}

public static class LoaiDanhGia
{
    public const string NhaHang = "NhaHang";
    public const string MonAn = "MonAn";
    public const string NhanVien = "NhanVien";

    public static readonly string[] TatCa = { NhaHang, MonAn, NhanVien };

    public static string TenHienThi(string? loai) => loai?.Trim() switch
    {
        MonAn => "Món ăn",
        NhanVien => "Nhân viên",
        _ => "Nhà hàng"
    };
}
