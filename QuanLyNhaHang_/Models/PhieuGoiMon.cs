using System;
using System.Collections.Generic;

namespace QuanLyNhaHang_.Models;

public partial class PhieuGoiMon
{
    public string Mapgm { get; set; } = null!;

    public string? Maban { get; set; }

    public string? Manv { get; set; }

    public DateTime? Ngaylap { get; set; }

    public string? Trangthai { get; set; }

    // Khách của phiếu (lưu ngay trên phiếu để luôn biết phiếu thuộc khách / tài khoản nào)
    public string? Makh { get; set; }

    public string? Tenkhach { get; set; }

    public string? Sdtkhach { get; set; }

    public virtual KhachHang? MakhNavigation { get; set; }
}
