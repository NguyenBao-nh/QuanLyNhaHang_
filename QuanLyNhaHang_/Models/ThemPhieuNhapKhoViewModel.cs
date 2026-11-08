namespace QuanLyNhaHang_.Models
{
    // ===== Dữ liệu GỬI LÊN từ form (tất cả nullable để không bị ModelState báo "required" ngầm) =====
    public class ChiTietNhapKhoInput
    {
        public string? Manguyenlieu { get; set; }
        public int? Soluong { get; set; }
    }

    public class ThemPhieuNhapKhoInput
    {
        public string? MaNhapKho { get; set; }
        public string? MaNV { get; set; }
        public DateTime? NgayNhapKho { get; set; }
        public List<ChiTietNhapKhoInput>? ChiTiet { get; set; }

        // true: trả tiền hàng bằng tiền mặt lấy từ két (trừ vào két của ca đang mở)
        public bool TraTienMat { get; set; }
    }

    // ===== Dữ liệu HIỂN THỊ ra form =====
    public class NguyenLieuOption
    {
        public string Ma { get; set; } = "";
        public string Ten { get; set; } = "";
        public string Dvt { get; set; } = "";
        public int DonGia { get; set; }
    }

    public class NhanVienOption
    {
        public string Ma { get; set; } = "";
        public string HoTen { get; set; } = "";
    }

    public class ThemPhieuNhapKhoViewModel
    {
        public string MaNhapKho { get; set; } = "";

        // Có giá trị khi đăng nhập bằng tài khoản nhân viên -> khóa ô, ngược lại cho chọn từ danh sách
        public string? MaNVSession { get; set; }
        public string? MaNV { get; set; }

        public DateTime? NgayNhapKho { get; set; }

        public List<NguyenLieuOption> NguyenLieus { get; set; } = new();
        public List<NhanVienOption> NhanViens { get; set; } = new();

        // Các dòng đã nhập (dùng khi lỗi và hiển thị lại form)
        public List<ChiTietNhapKhoInput> ChiTiet { get; set; } = new();

        public bool TraTienMat { get; set; }

        // Tiền két hiện tại (null = chưa mở ca)
        public int? TienKet { get; set; }
    }
}
