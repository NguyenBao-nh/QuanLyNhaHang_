using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class HoaDon
{
    public string Mahoadon { get; set; } = null!;

    public string? Maban { get; set; }

    public string? Makh { get; set; }

    public string? Manv { get; set; }

    public string? Magiamgia { get; set; }

    public DateTime? Ngaylap { get; set; }

    public int? Tongtien { get; set; }

    public string? Hinhthuc { get; set; }

    // Tên / SĐT khách lưu ngay trên hóa đơn (khách vãng lai không có MAKH vẫn giữ được thông tin)
    public string? Tenkhach { get; set; }

    public string? Sdtkhach { get; set; }

    // Đơn trực tuyến (khách đăng nhập, đặt món + thanh toán trên web) - xem LoaiDon / TrangThaiDon
    public string? Loaidon { get; set; }

    public string? Trangthai { get; set; }

    public string? Diachigiao { get; set; }

    public string? Ghichu { get; set; }

    public DateTime? Tgxacnhan { get; set; }

    public DateTime? Tggiao { get; set; }

    public string? Lydohuy { get; set; }

    // Chứng từ thanh toán trước của đơn trực tuyến (chống "bom hàng")
    public string? Anhchuyenkhoan { get; set; }

    public string? Magiaodich { get; set; }

    // Ca làm việc lập hóa đơn (đơn trực tuyến: ca lúc xác nhận nhận tiền)
    public string? Maca { get; set; }

    // Đơn trực tuyến: có LOAIDON = TrucTuyen; hóa đơn cũ chưa có cột này thì nhận theo "không có nhân viên + có khách"
    public bool LaDonTrucTuyen => Loaidon?.Trim() == LoaiDon.TrucTuyen
        || (string.IsNullOrWhiteSpace(Loaidon) && string.IsNullOrWhiteSpace(Manv) && !string.IsNullOrWhiteSpace(Makh));

    // Tên hiển thị: khách có tài khoản -> họ tên, vãng lai -> tên đã ghi
    public string TenHienThi => MakhNavigation?.Hoten ?? (string.IsNullOrWhiteSpace(Tenkhach) ? "Khách vãng lai" : Tenkhach);

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual ICollection<DanhGia> DanhGia { get; set; } = new List<DanhGia>();

    public virtual Ban? MabanNavigation { get; set; }

    public virtual KhachHang? MakhNavigation { get; set; }

    public virtual NhanVien? ManvNavigation { get; set; }
}


public static class LoaiDon
{
    public const string TaiQuan = "TaiQuan";
    public const string TrucTuyen = "TrucTuyen";
}

// Trạng thái đơn trực tuyến: Chờ xác nhận -> Đã xác nhận -> Đã giao (hoặc Đã hủy). Hóa đơn tại quán: Hoàn thành.
public static class TrangThaiDon
{
    public const string ChoXacNhan = "Chờ xác nhận";
    public const string DaXacNhan = "Đã xác nhận";
    public const string DaGiao = "Đã giao";
    public const string DaHuy = "Đã hủy";
    public const string HoanThanh = "Hoàn thành";

    public static string MauBadge(string? tt) => tt?.Trim() switch
    {
        ChoXacNhan => "bg-warning text-dark",
        DaXacNhan => "bg-info text-dark",
        DaGiao => "bg-success",
        DaHuy => "bg-danger",
        _ => "bg-secondary"
    };
}

// Dữ liệu cho partial Views/Admin_DonTrucTuyen/_NutXuLy.cshtml
public class NutXuLyDonModel
{
    public HoaDon Don { get; set; } = null!;
    public string? Tab { get; set; }
    public bool VeChiTiet { get; set; }
}
