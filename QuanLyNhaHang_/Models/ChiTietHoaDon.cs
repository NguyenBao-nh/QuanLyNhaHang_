using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class ChiTietHoaDon
{
    public string Mahoadon { get; set; } = null!;

    public string Mamonan { get; set; } = null!;

    public int? Soluong { get; set; }

    public int? Thanhtien { get; set; }

    public DateTime? Tghoanthanh { get; set; }

    public virtual HoaDon MahoadonNavigation { get; set; } = null!;

    public virtual MonAn MamonanNavigation { get; set; } = null!;
}
