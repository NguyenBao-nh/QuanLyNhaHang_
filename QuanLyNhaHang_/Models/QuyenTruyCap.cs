using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class QuyenTruyCap
{
    public string Maquyen { get; set; } = null!;

    public string? Tenquyen { get; set; }

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
