using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class KhachHang
{
    public string Makh { get; set; } = null!;

    public string? Mataikhoan { get; set; }

    public string? Hoten { get; set; }

    public DateOnly? Ngaysinh { get; set; }

    public string? Sodt { get; set; }

    public int? Diemthanhvien { get; set; }

    public string? Avatar { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual TaiKhoan? MataikhoanNavigation { get; set; }
}
