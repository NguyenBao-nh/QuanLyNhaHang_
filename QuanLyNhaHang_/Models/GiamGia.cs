using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class GiamGia
{
    public string Magiamgia { get; set; } = null!;

    public string? Code { get; set; }

    public DateTime? Ngaybd { get; set; }

    public DateTime? Ngaykt { get; set; }

    public int? Soluong { get; set; }

}
