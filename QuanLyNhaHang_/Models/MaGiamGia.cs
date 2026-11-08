using System;

namespace QuanLyNhaHang_.Models
{
    public class MaGiamGia
    {
        public string MaGiamGia_Code { get; set; } = null!;
        public DateTime NgayBD { get; set; }
        public DateTime NgayKT { get; set; }
        public int SoLuong { get; set; }

        // PhanTram > 0: SoTien là số tiền giảm TỐI ĐA (0 = không giới hạn).
        // PhanTram = 0: giảm cố định SoTien (mã khách đổi từ điểm thành viên).
        public int SoTien { get; set; }

        // Phần trăm giảm (1 - 100). Mã do admin tạo bắt buộc nhập phần trăm.
        public int PhanTram { get; set; }

        /// <summary>Số tiền được giảm cho hóa đơn có tổng <paramref name="tong"/> (không vượt quá tổng).</summary>
        public int TinhSoTienGiam(int tong)
        {
            if (tong <= 0) return 0;
            int giam;
            if (PhanTram > 0)
            {
                giam = (int)Math.Round(tong * (Math.Min(PhanTram, 100) / 100m), MidpointRounding.AwayFromZero);
                if (SoTien > 0) giam = Math.Min(giam, SoTien);
            }
            else
            {
                giam = Math.Max(0, SoTien);
            }
            return Math.Min(giam, tong);
        }

        public string MoTa => PhanTram > 0
            ? $"Giảm {PhanTram}%" + (SoTien > 0 ? $" (tối đa {SoTien:N0}đ)" : "")
            : $"Giảm {SoTien:N0}đ";
    }
}
