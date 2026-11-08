using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

// Tổng hợp tiền của một khoảng thời gian (1 ngày hoặc 1 tháng)
public class TongHopQuy
{
    public int SoHoaDon { get; set; }
    public long DoanhThuTienMat { get; set; }
    public long DoanhThuChuyenKhoan { get; set; }
    public long DoanhThu => DoanhThuTienMat + DoanhThuChuyenKhoan;

    public long NhapKho { get; set; }
    public long ChiVat { get; set; }
    public long TongChi => NhapKho + ChiVat;

    public long XuatKho { get; set; }

    // Thu khác ngoài hóa đơn: khách bồi thường (vỡ bát, hỏng đồ...) ghi riêng, không gắn hóa đơn.
    // Bồi thường gắn hóa đơn đã nằm trong tổng tiền hóa đơn (doanh thu) nên không cộng ở đây.
    public long ThuKhac { get; set; }

    // Thu - chi = doanh thu + thu khác - tiền chi tiêu
    public long ConLai => DoanhThu + ThuKhac - TongChi;

    // Doanh thu trung bình mỗi hóa đơn
    public long TrungBinhHoaDon => SoHoaDon == 0 ? 0 : DoanhThu / SoHoaDon;
}

// Một tháng: doanh thu, tiền chi tiêu (nhập kho + chi vặt), tiền hàng xuất kho
public class DongThangQuy : TongHopQuy
{
    public int Thang { get; set; }
}

// Một ngày trong tháng (bảng doanh thu theo ngày của quản lý)
public class DongNgayQuy : TongHopQuy
{
    public DateTime Ngay { get; set; }
}

// Một hóa đơn được tính doanh thu trong ngày đang xem
public class HoaDonTrongNgay
{
    public string MaHoaDon { get; set; } = "";
    public DateTime? NgayLap { get; set; }
    public string? LoaiDon { get; set; }
    public string? HinhThuc { get; set; }
    public string? TenKhach { get; set; }
    public string? MaBan { get; set; }
    public int TongTien { get; set; }
}

// Một khoản thu / chi tiền mặt ghi tay (bồi thường ngoài hóa đơn, chi tiêu vặt) trong ngày đang xem
public class ThuChiTrongNgay
{
    public int MaGD { get; set; }
    public string MaCa { get; set; } = "";
    public string Loai { get; set; } = "";
    public bool LaThu { get; set; }
    public int SoTien { get; set; }
    public string? NoiDung { get; set; }
    public string? MaChungTu { get; set; }
    public DateTime ThoiGian { get; set; }
    public string? TenNV { get; set; }
    // Chỉ xóa được khoản ghi nhầm của ca đang mở, không gắn hóa đơn
    public bool XoaDuoc { get; set; }
}

// Kết quả so sánh 2 tháng (chỉ quản lý)
public class SoSanhThangQuy
{
    public int ThangA { get; set; }
    public int NamA { get; set; }
    public int ThangB { get; set; }
    public int NamB { get; set; }
    public TongHopQuy A { get; set; } = new();
    public TongHopQuy B { get; set; } = new();

    // Chênh lệch A so với B: (A - B) / B
    public static string PhanTram(long a, long b)
    {
        if (b == 0) return a == 0 ? "0%" : "mới";
        double p = (a - b) * 100.0 / Math.Abs(b);
        return (p > 0 ? "+" : "") + p.ToString("0.#") + "%";
    }
}

// Dữ liệu trang Quỹ (tài khoản NHÂN VIÊN): tiền két, thu chi, doanh thu THEO NGÀY, chi tiêu theo tháng.
// Không có doanh thu theo tháng.
public class ThongKeQuy
{
    // Ngày đang xem (phần "Doanh thu theo ngày")
    public DateTime Ngay { get; set; } = DateTime.Today;
    public bool ChiXemHomNay { get; set; }
    public TongHopQuy NgayDangXem { get; set; } = new();
    public List<HoaDonTrongNgay> HoaDonNgay { get; set; } = new();

    // Phiếu thu / chi tiền mặt trong ngày (bồi thường, chi tiêu vặt) - ghi vào két của ca đang mở
    public List<ThuChiTrongNgay> ThuChiNgay { get; set; } = new();

    // Tiền trong két của ca đang mở (null = chưa mở ca)
    public CaLamViec? CaDangMo { get; set; }
    public int? TienKet { get; set; }

    // Chi tiêu theo tháng của năm đang xem (chỉ dùng các cột chi tiêu / xuất kho, không hiện doanh thu)
    public int Nam { get; set; }
    public int Thang { get; set; }
    public List<DongThangQuy> TheoThang { get; set; } = new();
    public DongThangQuy ThangDangXem => TheoThang.Count >= Thang ? TheoThang[Thang - 1] : new DongThangQuy { Thang = Thang };
}

// Dữ liệu trang Doanh thu (CHỈ tài khoản QUẢN LÝ): theo tháng, từng ngày trong tháng, so sánh 2 tháng
public class ThongKeDoanhThu
{
    public int Nam { get; set; }
    public int Thang { get; set; }

    public List<DongThangQuy> TheoThang { get; set; } = new();
    public List<DongNgayQuy> TheoNgayTrongThang { get; set; } = new();
    public SoSanhThangQuy SoSanh { get; set; } = new();

    public DongThangQuy ThangDangXem => TheoThang.Count >= Thang ? TheoThang[Thang - 1] : new DongThangQuy { Thang = Thang };

    public long DoanhThuNam => TheoThang.Sum(t => t.DoanhThu);
    public long NhapKhoNam => TheoThang.Sum(t => t.NhapKho);
    public long ChiVatNam => TheoThang.Sum(t => t.ChiVat);
    public long XuatKhoNam => TheoThang.Sum(t => t.XuatKho);
    public long ThuKhacNam => TheoThang.Sum(t => t.ThuKhac);
}
