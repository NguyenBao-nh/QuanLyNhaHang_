using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhaHang_.Models;

public partial class NguyenLieu
{
    public string Manguyenlieu { get; set; } = null!;

    public string? Makho { get; set; }

    public string? Tennguyenlieu { get; set; }

    public int? Dongia { get; set; }

    public string? Dvt { get; set; }

    // Không có cột này trong bảng NguyenLieu; được tính = tổng nhập - tổng xuất (xem XuLyNguyenLieu)
    [NotMapped]
    public int? Soluongton { get; set; }


    public virtual ICollection<ChiTietNhapKho> ChiTietNhapKhos { get; set; } = new List<ChiTietNhapKho>();

    public virtual ICollection<ChiTietXuatKho> ChiTietXuatKhos { get; set; } = new List<ChiTietXuatKho>();

    public virtual Kho? MakhoNavigation { get; set; }
}