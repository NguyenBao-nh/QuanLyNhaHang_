using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

// Một ca làm việc (sáng / chiều / tối). Khi đóng ca các cột chốt được ghi lại -> dùng làm phiếu đóng ca.
public partial class CaLamViec
{
    public string Maca { get; set; } = null!;

    public string Loaica { get; set; } = null!;

    public DateTime Ngay { get; set; }

    public string Trangthai { get; set; } = null!;

    public string? Manvmo { get; set; }

    public string? Tennvmo { get; set; }

    public DateTime Tgmo { get; set; }

    public int Tiendauca { get; set; }

    public string? Manvdong { get; set; }

    public string? Tennvdong { get; set; }

    public DateTime? Tgdong { get; set; }

    public int? Sohoadon { get; set; }

    public int? Doanhthu { get; set; }

    public int? Thutienmat { get; set; }

    public int? Thuchuyenkhoan { get; set; }

    public int? Tongchi { get; set; }

    public int? Tongrut { get; set; }

    public int? Tienketcuoi { get; set; }

    public int? Tienthucte { get; set; }

    public string? Ghichu { get; set; }

    public bool DangMo => Trangthai?.Trim() == TrangThaiCa.DangMo;
}

// Tiền vào / ra két trong ca
public partial class GiaoDichKet
{
    public int Magd { get; set; }

    public string Maca { get; set; } = null!;

    public string Loai { get; set; } = null!;

    public int Sotien { get; set; }

    public string? Noidung { get; set; }

    public string? Machungtu { get; set; }

    public DateTime Thoigian { get; set; }

    public string? Manv { get; set; }

    // Tiền vào két (+) hay ra khỏi két (-)
    public bool LaTienVao => Loai?.Trim() is LoaiGiaoDichKet.ThuBan or LoaiGiaoDichKet.ThuBoiThuong;
}

public static class TrangThaiCa
{
    public const string DangMo = "Đang mở";
    public const string DaDong = "Đã đóng";
}

public static class LoaiCa
{
    public const string Sang = "Sáng";
    public const string Chieu = "Chiều";
    public const string Toi = "Tối";

    public static readonly string[] TatCa = { Sang, Chieu, Toi };

    // Gợi ý ca theo giờ hiện tại: trước 11h sáng, 11h - 17h chiều, sau đó tối
    public static string TheoGio(DateTime t) => t.Hour < 11 ? Sang : t.Hour < 17 ? Chieu : Toi;
}

public static class LoaiGiaoDichKet
{
    public const string ThuBan = "ThuBan";     // khách trả tiền mặt (+)
    public const string ThuBoiThuong = "ThuBoiThuong"; // khách bồi thường (vỡ bát, hỏng đồ...) (+)
    public const string ChiTieu = "ChiTieu";   // chi tiêu vặt (-)
    public const string NhapKho = "NhapKho";   // trả tiền hàng nhập kho bằng tiền mặt (-)
    public const string RutTien = "RutTien";   // két đầy, rút tiền ra (-)

    public static string Ten(string? loai) => loai?.Trim() switch
    {
        ThuBan => "Thu tiền mặt",
        ThuBoiThuong => "Thu bồi thường",
        ChiTieu => "Chi tiêu",
        NhapKho => "Trả tiền nhập kho",
        RutTien => "Rút tiền khỏi két",
        _ => loai ?? ""
    };
}

public static class HinhThucThanhToan
{
    public const string TienMat = "Tiền mặt";
    public const string ChuyenKhoan = "Chuyển khoản";

    public static bool LaTienMat(string? h) => h != null && h.Trim().Equals(TienMat, StringComparison.OrdinalIgnoreCase);
}

// Dữ liệu trang Admin_Ca/Index
public class TrangCaViewModel
{
    public CaLamViec? CaDangMo { get; set; }
    public QuanLyNhaHang_.Services.XuLyKet.TongKetCa? TongKet { get; set; }
    public List<GiaoDichKet> GiaoDich { get; set; } = new();

    // Khi chưa mở ca
    public CaLamViec? CaTruoc { get; set; }
    public int GoiYTienDauCa { get; set; }
    public string GoiYLoaiCa { get; set; } = LoaiCa.Sang;
    public List<string> CaDaMoHomNay { get; set; } = new();
}
