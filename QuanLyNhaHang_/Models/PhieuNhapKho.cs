using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class PhieuNhapKho
{
    public string Manhapkho { get; set; } = null!;

    public string? Manv { get; set; }

    public DateTime? Ngaynk { get; set; }

    public int? Tongtien { get; set; }

    public virtual ICollection<ChiTietNhapKho> ChiTietNhapKhos { get; set; } = new List<ChiTietNhapKho>();

    public virtual NhanVien? ManvNavigation { get; set; }
}
