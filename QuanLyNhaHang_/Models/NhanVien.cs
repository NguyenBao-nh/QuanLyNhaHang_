using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class NhanVien
{
    public string Manv { get; set; } = null!;

    public string Mataikhoan { get; set; } = null!;

    public string? Hoten { get; set; }

    public string? Chucvu { get; set; }

    public string? Sodt { get; set; }

    public int? Luong { get; set; }

    public int? Sogiolam { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual TaiKhoan MataikhoanNavigation { get; set; } = null!;

    public virtual ICollection<PhieuNhapKho> PhieuNhapKhos { get; set; } = new List<PhieuNhapKho>();

    public virtual ICollection<PhieuXuatKho> PhieuXuatKhos { get; set; } = new List<PhieuXuatKho>();
}
