namespace QuanLyNhaHang_.Models
{
    // ===== Dữ liệu GỬI LÊN từ form (nullable để không bị ModelState báo "required" ngầm) =====
    public class ChiTietXuatKhoInput
    {
        public string? Manguyenlieu { get; set; }
        public int? Soluong { get; set; }
    }

    public class ThemPhieuXuatKhoInput
    {
        public string? MaXuatKho { get; set; }
        public string? MaNV { get; set; }
        public DateTime? NgayXuatKho { get; set; }
        public List<ChiTietXuatKhoInput>? ChiTiet { get; set; }
    }

    // ===== Dữ liệu HIỂN THỊ ra form =====
    public class NguyenLieuXuatOption
    {
        public string Ma { get; set; } = "";
        public string Ten { get; set; } = "";
        public string Dvt { get; set; } = "";
        public int DonGia { get; set; }
        public int Ton { get; set; }
    }

    public class NhanVienXuatOption
    {
        public string Ma { get; set; } = "";
        public string HoTen { get; set; } = "";
    }

    public class ThemPhieuXuatKhoViewModel
    {
        public string MaXuatKho { get; set; } = "";

        // Có giá trị khi đăng nhập bằng tài khoản nhân viên -> khóa ô, ngược lại cho chọn từ danh sách
        public string? MaNVSession { get; set; }
        public string? MaNV { get; set; }

        public DateTime? NgayXuatKho { get; set; }

        public List<NguyenLieuXuatOption> NguyenLieus { get; set; } = new();
        public List<NhanVienXuatOption> NhanViens { get; set; } = new();

        // Các dòng đã nhập (dùng khi lỗi và hiển thị lại form)
        public List<ChiTietXuatKhoInput> ChiTiet { get; set; } = new();
    }
}
