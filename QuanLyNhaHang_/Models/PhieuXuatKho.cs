using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class PhieuXuatKho
{
    public string Maxuatkho { get; set; } = null!;

    public string Manv { get; set; } = null!;

    public DateTime? Ngayxk { get; set; }

    public int? Tongtien { get; set; }

    public virtual ICollection<ChiTietXuatKho> ChiTietXuatKhos { get; set; } = new List<ChiTietXuatKho>();

    public virtual NhanVien ManvNavigation { get; set; } = null!;
}
