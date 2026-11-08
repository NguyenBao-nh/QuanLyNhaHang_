using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class ChiTietNhapKho
{
    public string Manhapkho { get; set; } = null!;

    public string Manguyenlieu { get; set; } = null!;

    public int? Soluong { get; set; }

    public int? Thanhtien { get; set; }

    public virtual NguyenLieu ManguyenlieuNavigation { get; set; } = null!;

    public virtual PhieuNhapKho ManhapkhoNavigation { get; set; } = null!;
}
