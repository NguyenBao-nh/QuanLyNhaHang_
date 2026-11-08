using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuanLyNhaHang_.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace QuanLyNhaHang_.Models
{
    public class KetNoiTaiKhoan
    {
        private readonly string _connectionString;

        // Constructor hỗ trợ lấy connection string từ appsettings.json của .NET Core
        public KetNoiTaiKhoan(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connect1")
                ?? "Server=your_server;Database=your_db;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        // Constructor không tham số (phòng hờ nếu bạn khởi tạo kiểu new KetNoiTaiKhoan())
        public KetNoiTaiKhoan()
        {
            _connectionString = "Server=your_server;Database=your_db;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        // =========================================================================
        // ĐĂNG KÝ TÀI KHOẢN
        // =========================================================================
        public bool DangKy(TaiKhoan taiKhoan, string? baseUrl = null)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string token = Guid.NewGuid().ToString();

                string query = @"
                    INSERT INTO TaiKhoan (MaTaiKhoan, MaQuyen, TenDangNhap, MatKhau, Email, IsEmailConfirmed, EmailConfirmationToken) 
                    VALUES (@MaTaiKhoan, @MaQuyen, @TenDangNhap, @MatKhau, @Email, 0, @EmailConfirmationToken)";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.Add(new SqlParameter("@MaTaiKhoan", GenerateMaTaiKhoan()));
                cmd.Parameters.Add(new SqlParameter("@MaQuyen", "Q001"));
                cmd.Parameters.Add(new SqlParameter("@TenDangNhap", taiKhoan.Tendangnhap ?? string.Empty));
                cmd.Parameters.Add(new SqlParameter("@MatKhau", taiKhoan.Matkhau ?? string.Empty));
                cmd.Parameters.Add(new SqlParameter("@Email", taiKhoan.Email ?? string.Empty));
                cmd.Parameters.Add(new SqlParameter("@EmailConfirmationToken", token));

                conn.Open();
                int result = cmd.ExecuteNonQuery();
                conn.Close();

                if (result > 0 && !string.IsNullOrEmpty(taiKhoan.Email))
                {
                    string goc = (baseUrl ?? "https://localhost:7122").TrimEnd('/');
                    string confirmationLink = $"{goc}/Account/ConfirmEmail?email={Uri.EscapeDataString(taiKhoan.Email)}&token={Uri.EscapeDataString(token)}";
                    SendConfirmationEmail(taiKhoan.Email, confirmationLink);
                }

                return result > 0;
            }
        }

        // =========================================================================
        // GỬI EMAIL XÁC THỰC
        // =========================================================================
        public bool SendConfirmationEmail(string email, string link)
        {
            try
            {
                using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("chaytue0203@gmail.com", "kctw ltds teaj luvb"),
                    EnableSsl = true,
                    Timeout = 15000   // 15 giây, tránh treo trang đăng ký khi mạng/SMTP lỗi
                };

                using var mailMessage = new MailMessage("khangtuong040@gmail.com", email)
                {
                    Subject = "Xác thực email",
                    Body = $@"
                    <html>
                    <body style='text-align: center; font-family: Arial, sans-serif;'>
                        <div style='margin: 20px;'>
                            <h2 style='color: red;'>FREEDOM</h2>
                            <p>Vui lòng nhấn vào liên kết sau để xác thực tài khoản của bạn:</p>
                            <a href='{link}' style='color: blue;'>{link}</a>
                        </div>
                    </body>
                    </html>",
                    IsBodyHtml = true
                };

                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi mail xác thực: " + ex.Message);
                return false;
            }
        }

        // =========================================================================
        // GỬI MÃ OTP
        // =========================================================================
        public void SendOtpEmail(string email, string otp)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(email);
                mail.From = new MailAddress("khangtuong040@gmail.com");
                mail.Subject = "Mã OTP để tạo lại mật khẩu";
                mail.Body = $"Mã OTP của bạn là: <b>{otp}</b>";
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("chaytue0203@gmail.com", "kctw ltds teaj luvb"),
                    EnableSsl = true
                };

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi OTP: " + ex.Message);
            }
        }

        // =========================================================================
        // TẠO MÃ TÀI KHOẢN TỰ ĐỘNG
        // =========================================================================
        public int GetNextNumberFromDatabase()
        {
            int nextNumber = 1;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT MAX(CAST(SUBSTRING(MaTaiKhoan, 3, LEN(MaTaiKhoan) - 2) AS INT)) AS MaxValue FROM TaiKhoan;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        nextNumber = Convert.ToInt32(result) + 1;
                    }
                }
            }

            return nextNumber;
        }

        public string GenerateMaTaiKhoan()
        {
            string prefix = "TK";
            int nextNumber = GetNextNumberFromDatabase();
            return prefix + nextNumber.ToString("D4");
        }

        // =========================================================================
        // ĐĂNG NHẬP (LẤY DANH SÁCH TÀI KHOẢN)
        // =========================================================================
        public List<TaiKhoan> DangNhap()
        {
            List<TaiKhoan> listProduct = new List<TaiKhoan>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT 
                        MaTaiKhoan, 
                        MaQuyen, 
                        TenDangNhap, 
                        MatKhau, 
                        Email,
                        IsEmailConfirmed,
                        EmailConfirmationToken
                    FROM TaiKhoan";

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.CommandType = CommandType.Text;

                con.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        TaiKhoan tk = new TaiKhoan();
                        tk.Mataikhoan = rdr.GetValue(0).ToString()!;
                        tk.Maquyen = rdr.GetValue(1).ToString();
                        tk.Tendangnhap = rdr.GetValue(2).ToString();
                        tk.Matkhau = rdr.GetValue(3).ToString();
                        tk.Email = rdr.GetValue(4).ToString();
                        tk.Isemailconfirmed = rdr != null && rdr.GetValue(5) != DBNull.Value ? Convert.ToInt32(rdr.GetValue(5)) : 0;

                        listProduct.Add(tk);
                    }
                }
            }
            return listProduct;
        }

        // =========================================================================
        // GỬI MAIL MÃ GIẢM GIÁ
        // =========================================================================
        public void GuiMailMaGiamGia(string email, string randomString, decimal soTien, DateTime NgayBD, DateTime NgayKT, int DiemThanhVien)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(email);
                mail.From = new MailAddress("khangtuong040@gmail.com");
                mail.Subject = "Thông tin mã giảm giá";
                mail.IsBodyHtml = true;

                mail.Body = $@"
                    <h3>Chào bạn,</h3>
                    <p>Chúng tôi vui mừng thông báo về mã giảm giá của bạn:</p>
                    <p><strong>Mã giảm giá: </strong>{randomString}</p>
                    <p><strong>Số tiền giảm: </strong>{soTien}</p>
                    <p><strong>Ngày bắt đầu: </strong>{NgayBD:dd/MM/yyyy}</p>
                    <p><strong>Ngày kết thúc: </strong>{NgayKT:dd/MM/yyyy}</p>
                    <p><strong>Điểm thành viên của bạn: </strong>{DiemThanhVien}</p>
                    <p>Hãy sử dụng mã giảm giá này để được ưu đãi tại cửa hàng của chúng tôi!</p>
                ";

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("chaytue0203@gmail.com", "kctw ltds teaj luvb"),
                    EnableSsl = true
                };

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi gửi email giảm giá: " + ex.Message);
            }
        }
    }
}
