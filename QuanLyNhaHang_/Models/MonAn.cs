using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class MonAn
{
    public string Mamonan { get; set; } = null!;

    public string? Tenmon { get; set; }

    public string? Loaimon { get; set; }

    public double? Gia { get; set; }

    public string? AnhMon { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
}