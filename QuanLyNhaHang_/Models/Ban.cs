using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class Ban
{
    public string Maban { get; set; } = null!;

    public string? Trangthai { get; set; }

    // Khách đang ngồi ở bàn: khách có tài khoản (MAKH) hoặc khách vãng lai (chỉ có tên / SĐT)
    public string? Makh { get; set; }

    public string? Tenkhach { get; set; }

    public string? Sdtkhach { get; set; }

    // Thời điểm khách nhận bàn
    public DateTime? Giovao { get; set; }

    public virtual KhachHang? MakhNavigation { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
}
