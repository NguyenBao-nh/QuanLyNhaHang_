using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhaHang_.Models;

public partial class TaiKhoan
{
    public string Mataikhoan { get; set; } = null!;

    public string? Maquyen { get; set; }

    public string? Tendangnhap { get; set; }

    public string? Matkhau { get; set; }

    public string? Email { get; set; }

    public int? Isemailconfirmed { get; set; }

    public string? Emailconfirmationtoken { get; set; }

    public int? Otp { get; set; }

    [NotMapped]
    public string? XacNhanMatKhau { get; set; }

    public virtual ICollection<KhachHang> KhachHangs { get; set; } = new List<KhachHang>();

    public virtual QuyenTruyCap? MaquyenNavigation { get; set; }

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}