using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaHang_.Models;

// Form thêm / sửa nhân viên (kèm tài khoản đăng nhập) - chỉ quản lý dùng
public class NhanVienForm
{
    // null / rỗng = thêm mới
    public string? Manv { get; set; }
    public string? Mataikhoan { get; set; }
    public bool LaThemMoi => string.IsNullOrWhiteSpace(Manv);

    [Required(ErrorMessage = "Nhập họ tên.")]
    [StringLength(20, ErrorMessage = "Họ tên tối đa 20 ký tự.")]
    [Display(Name = "Họ tên")]
    public string? Hoten { get; set; }

    [StringLength(20, ErrorMessage = "Chức vụ tối đa 20 ký tự.")]
    [Display(Name = "Chức vụ")]
    public string? Chucvu { get; set; }

    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại gồm 10-11 chữ số, bắt đầu bằng 0.")]
    [Display(Name = "Số điện thoại")]
    public string? Sodt { get; set; }

    [Range(0, 1_000_000_000, ErrorMessage = "Lương không hợp lệ.")]
    [Display(Name = "Lương")]
    public int? Luong { get; set; }

    [Range(0, 10_000, ErrorMessage = "Số giờ làm không hợp lệ.")]
    [Display(Name = "Số giờ làm")]
    public int? Sogiolam { get; set; }

    [Required(ErrorMessage = "Nhập tên đăng nhập.")]
    [StringLength(20, ErrorMessage = "Tên đăng nhập tối đa 20 ký tự.")]
    [Display(Name = "Tên đăng nhập")]
    public string? Tendangnhap { get; set; }

    // Thêm mới: bắt buộc. Sửa: để trống = giữ mật khẩu cũ.
    [StringLength(20, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 20 ký tự.")]
    [Display(Name = "Mật khẩu")]
    public string? Matkhau { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(40, ErrorMessage = "Email tối đa 40 ký tự.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Chọn quyền.")]
    [Display(Name = "Quyền")]
    public string? Maquyen { get; set; }
}

// Một dòng trong danh sách nhân viên
public class DongNhanVien
{
    public NhanVien NhanVien { get; set; } = null!;
    public string? Tendangnhap { get; set; }
    public string? Email { get; set; }
    public string? Maquyen { get; set; }
    public string? Tenquyen { get; set; }
    public bool LaQuanLy { get; set; }
}
