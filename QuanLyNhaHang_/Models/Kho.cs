using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // Thêm thư viện này

namespace QuanLyNhaHang_.Models;

public partial class Kho
{
    [Column("MAKHO")]
    public string Makho { get; set; } = null!;

    // Ép trực tiếp tên cột phía trên property bằng [Column]
    [Column("SOLUONGTON")]
    public int? Soluongton { get; set; }

    public virtual ICollection<NguyenLieu> NguyenLieus { get; set; } = new List<NguyenLieu>();
}