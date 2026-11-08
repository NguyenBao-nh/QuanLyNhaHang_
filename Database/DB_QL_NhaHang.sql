-- CSDL nhà hàng FREEDOM: tạo database + toàn bộ bảng + dữ liệu mẫu.
-- Cách dùng: mở file trong SSMS và Execute (F5) 1 lần trên máy CHƯA có DB_QL_NhaHang
-- (nếu đã có DB cũ thì xóa DB cũ trước, vì script tạo lại các bảng).
-- Tài khoản mẫu (mật khẩu 123456): admin (quản lý), nhanvien (nhân viên), khachhang (khách hàng).
IF DB_ID('DB_QL_NhaHang') IS NULL
    CREATE DATABASE DB_QL_NhaHang;
GO
USE [DB_QL_NhaHang]
GO
/****** Object:  Table [dbo].[Ban]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Ban](
	[MABAN] [char](10) NOT NULL,
	[TRANGTHAI] [nvarchar](30) NULL,
	[MAKH] [char](10) NULL,              -- khách (tài khoản) đang ngồi
	[TENKHACH] [nvarchar](50) NULL,      -- tên khách (cả khách vãng lai)
	[SDTKHACH] [varchar](11) NULL,
	[GIOVAO] [datetime] NULL,            -- lúc khách nhận bàn
PRIMARY KEY CLUSTERED 
(
	[MABAN] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChiTietGoiMon]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChiTietGoiMon](
	[MAPGM] [varchar](10) NOT NULL,
	[MAMONAN] [varchar](10) NOT NULL,
	[SOLUONG] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MAPGM] ASC,
	[MAMONAN] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChiTietHoaDon]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChiTietHoaDon](
	[MAHOADON] [char](10) NOT NULL,
	[MAMONAN] [char](10) NOT NULL,
	[SOLUONG] [int] NULL,
	[THANHTIEN] [int] NULL,
	[TGHOANTHANH] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[MAHOADON] ASC,
	[MAMONAN] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChiTietNhapKho]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChiTietNhapKho](
	[MANHAPKHO] [char](10) NOT NULL,
	[MANGUYENLIEU] [char](10) NOT NULL,
	[SOLUONG] [int] NULL,
	[THANHTIEN] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MANHAPKHO] ASC,
	[MANGUYENLIEU] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChiTietXuatKho]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChiTietXuatKho](
	[MAXUATKHO] [char](10) NOT NULL,
	[MANGUYENLIEU] [char](10) NOT NULL,
	[SOLUONG] [int] NULL,
	[THANHTIEN] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MAXUATKHO] ASC,
	[MANGUYENLIEU] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DanhGia]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DanhGia](
	[MADANHGIA] [char](10) NOT NULL,
	[MAHOADON] [char](10) NULL,
	[DANHGIA] [nvarchar](1000) NULL,
	[MATAIKHOAN] [char](10) NULL,        -- tài khoản viết đánh giá
	[LOAIDANHGIA] [nvarchar](20) NULL,   -- NhaHang | MonAn | NhanVien
	[MAMONAN] [char](10) NULL,
	[MANV] [char](10) NULL,
	[SOSAO] [int] NULL CONSTRAINT [CK_DG_SOSAO] CHECK ([SOSAO] IS NULL OR [SOSAO] BETWEEN 1 AND 5),
	[HINHANH] [varchar](200) NULL,
	[NGAYDANHGIA] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[MADANHGIA] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GiamGia]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GiamGia](
	[MAGIAMGIA] [char](10) NOT NULL,
	[CODE] [varchar](20) NULL,
	[NGAYBD] [datetime] NULL,
	[NGAYKT] [datetime] NULL,
	[SOLUONG] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MAGIAMGIA] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[HoaDon]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HoaDon](
	[MAHOADON] [char](10) NOT NULL,
	[MABAN] [char](10) NULL,
	[MAKH] [char](10) NULL,
	[MANV] [char](10) NULL,
	[MAGIAMGIA] [nvarchar](50) NULL,
	[NGAYLAP] [datetime] NULL,
	[TONGTIEN] [int] NULL,
	[HINHTHUC] [nvarchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[MAHOADON] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[KhachHang]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[KhachHang](
	[MAKH] [char](10) NOT NULL,
	[MATAIKHOAN] [char](10) NULL,
	[HOTEN] [nvarchar](20) NULL,
	[NGAYSINH] [date] NULL,
	[SODT] [char](11) NULL,
	[DIEMTHANHVIEN] [int] NULL,
	[AVATAR] [varchar](1500) NULL,
PRIMARY KEY CLUSTERED 
(
	[MAKH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Kho]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Kho](
	[MAKHO] [char](10) NOT NULL,
	[SOLUONGTON] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MAKHO] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MaGiamGia]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MaGiamGia](
	[MaGiamGia_Code] [nvarchar](50) NOT NULL,
	[NgayBD] [datetime] NOT NULL,
	[NgayKT] [datetime] NOT NULL,
	[SoLuong] [int] NOT NULL,
	[SoTien] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaGiamGia_Code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MonAn]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MonAn](
	[MAMONAN] [char](10) NOT NULL,
	[TENMON] [nvarchar](20) NULL,
	[LOAIMON] [nvarchar](20) NULL,
	[GIA] [float] NULL,
	[AnhMon] [varchar](150) NULL,
	[MOTA] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[MAMONAN] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NguyenLieu]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NguyenLieu](
	[MANGUYENLIEU] [char](10) NOT NULL,
	[MAKHO] [char](10) NULL,
	[TENNGUYENLIEU] [nvarchar](30) NULL,
	[DONGIA] [int] NULL,
	[DVT] [nvarchar](10) NULL,
	[SoLuongTon] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MANGUYENLIEU] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NhanVien]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NhanVien](
	[MANV] [char](10) NOT NULL,
	[MATAIKHOAN] [char](10) NOT NULL,
	[HOTEN] [nvarchar](20) NULL,
	[CHUCVU] [nvarchar](20) NULL,
	[SODT] [char](11) NULL,
	[LUONG] [int] NULL,
	[SOGIOLAM] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MANV] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhieuDatBan]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhieuDatBan](
	[MAPHIEU] [varchar](10) NOT NULL,
	[MAKH] [varchar](10) NULL,
	[NGAYDAT] [datetime] NULL,
	[TENKH] [nvarchar](50) NULL,
	[SOLUONG] [int] NULL,
	[EMAIL] [varchar](50) NULL,
	[SDT] [varchar](11) NULL,
	[GIODAT] [varchar](20) NULL,
	[TRANGTHAI] [nvarchar](20) NULL,     -- Chờ xếp bàn | Đang phục vụ | Đã phục vụ | Đã hủy
	[MABAN] [varchar](10) NULL,          -- bàn đã xếp cho phiếu
PRIMARY KEY CLUSTERED 
(
	[MAPHIEU] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhieuGoiMon]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhieuGoiMon](
	[MAPGM] [varchar](10) NOT NULL,
	[MABAN] [varchar](10) NULL,
	[MANV] [varchar](10) NULL,
	[NGAYLAP] [datetime] NULL,
	[TRANGTHAI] [nvarchar](20) NULL,
	[MAKH] [char](10) NULL,              -- khách (tài khoản) của phiếu
	[TENKHACH] [nvarchar](50) NULL,      -- tên khách (cả khách vãng lai)
	[SDTKHACH] [varchar](11) NULL,
PRIMARY KEY CLUSTERED 
(
	[MAPGM] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhieuNhapKho]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhieuNhapKho](
	[MANHAPKHO] [char](10) NOT NULL,
	[MANV] [char](10) NULL,
	[NGAYNK] [datetime] NULL,
	[TONGTIEN] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MANHAPKHO] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhieuXuatKho]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhieuXuatKho](
	[MAXUATKHO] [char](10) NOT NULL,
	[MANV] [char](10) NOT NULL,
	[NGAYXK] [datetime] NULL,
	[TONGTIEN] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MAXUATKHO] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QuyenTruyCap]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QuyenTruyCap](
	[MAQUYEN] [char](10) NOT NULL,
	[TENQUYEN] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[MAQUYEN] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaiKhoan]    Script Date: 31/10/2026 4:14:10 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaiKhoan](
	[MATAIKHOAN] [char](10) NOT NULL,
	[MAQUYEN] [char](10) NULL,
	[TENDANGNHAP] [varchar](20) NULL,
	[MATKHAU] [varchar](20) NULL,
	[EMAIL] [varchar](40) NULL,
	[ISEMAILCONFIRMED] [int] NULL,
	[EMAILCONFIRMATIONTOKEN] [nvarchar](100) NULL,
	[OTP] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MATAIKHOAN] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NguyenLieu] ADD  DEFAULT ((0)) FOR [SoLuongTon]
GO
ALTER TABLE [dbo].[TaiKhoan] ADD  DEFAULT ((0)) FOR [ISEMAILCONFIRMED]
GO
ALTER TABLE [dbo].[ChiTietHoaDon]  WITH CHECK ADD  CONSTRAINT [FK_CTHD_HD] FOREIGN KEY([MAHOADON])
REFERENCES [dbo].[HoaDon] ([MAHOADON])
GO
ALTER TABLE [dbo].[ChiTietHoaDon] CHECK CONSTRAINT [FK_CTHD_HD]
GO
ALTER TABLE [dbo].[ChiTietHoaDon]  WITH CHECK ADD  CONSTRAINT [FK_CTHD_MA] FOREIGN KEY([MAMONAN])
REFERENCES [dbo].[MonAn] ([MAMONAN])
GO
ALTER TABLE [dbo].[ChiTietHoaDon] CHECK CONSTRAINT [FK_CTHD_MA]
GO
ALTER TABLE [dbo].[ChiTietNhapKho]  WITH CHECK ADD  CONSTRAINT [FK_CTNK_NK] FOREIGN KEY([MANHAPKHO])
REFERENCES [dbo].[PhieuNhapKho] ([MANHAPKHO])
GO
ALTER TABLE [dbo].[ChiTietNhapKho] CHECK CONSTRAINT [FK_CTNK_NK]
GO
ALTER TABLE [dbo].[ChiTietNhapKho]  WITH CHECK ADD  CONSTRAINT [FK_CTNK_NL] FOREIGN KEY([MANGUYENLIEU])
REFERENCES [dbo].[NguyenLieu] ([MANGUYENLIEU])
GO
ALTER TABLE [dbo].[ChiTietNhapKho] CHECK CONSTRAINT [FK_CTNK_NL]
GO
ALTER TABLE [dbo].[ChiTietXuatKho]  WITH CHECK ADD  CONSTRAINT [FK_CTXK_NL] FOREIGN KEY([MANGUYENLIEU])
REFERENCES [dbo].[NguyenLieu] ([MANGUYENLIEU])
GO
ALTER TABLE [dbo].[ChiTietXuatKho] CHECK CONSTRAINT [FK_CTXK_NL]
GO
ALTER TABLE [dbo].[ChiTietXuatKho]  WITH CHECK ADD  CONSTRAINT [FK_CTXK_XK] FOREIGN KEY([MAXUATKHO])
REFERENCES [dbo].[PhieuXuatKho] ([MAXUATKHO])
GO
ALTER TABLE [dbo].[ChiTietXuatKho] CHECK CONSTRAINT [FK_CTXK_XK]
GO
ALTER TABLE [dbo].[DanhGia]  WITH CHECK ADD  CONSTRAINT [FK_DG_HD] FOREIGN KEY([MAHOADON])
REFERENCES [dbo].[HoaDon] ([MAHOADON])
GO
ALTER TABLE [dbo].[DanhGia] CHECK CONSTRAINT [FK_DG_HD]
GO
ALTER TABLE [dbo].[DanhGia]  WITH CHECK ADD  CONSTRAINT [FK_DG_TK] FOREIGN KEY([MATAIKHOAN])
REFERENCES [dbo].[TaiKhoan] ([MATAIKHOAN])
GO
ALTER TABLE [dbo].[DanhGia]  WITH CHECK ADD  CONSTRAINT [FK_DG_MA] FOREIGN KEY([MAMONAN])
REFERENCES [dbo].[MonAn] ([MAMONAN])
GO
ALTER TABLE [dbo].[DanhGia]  WITH CHECK ADD  CONSTRAINT [FK_DG_NV] FOREIGN KEY([MANV])
REFERENCES [dbo].[NhanVien] ([MANV])
GO
ALTER TABLE [dbo].[Ban]  WITH CHECK ADD  CONSTRAINT [FK_BAN_KH] FOREIGN KEY([MAKH])
REFERENCES [dbo].[KhachHang] ([MAKH])
GO
ALTER TABLE [dbo].[HoaDon]  WITH CHECK ADD  CONSTRAINT [FK_HD_BAN] FOREIGN KEY([MABAN])
REFERENCES [dbo].[Ban] ([MABAN])
GO
ALTER TABLE [dbo].[PhieuGoiMon]  WITH CHECK ADD  CONSTRAINT [FK_PGM_KH] FOREIGN KEY([MAKH])
REFERENCES [dbo].[KhachHang] ([MAKH])
GO
ALTER TABLE [dbo].[HoaDon] CHECK CONSTRAINT [FK_HD_BAN]
GO
ALTER TABLE [dbo].[HoaDon]  WITH NOCHECK ADD  CONSTRAINT [FK_HD_GG] FOREIGN KEY([MAGIAMGIA])
REFERENCES [dbo].[MaGiamGia] ([MaGiamGia_Code])
GO
ALTER TABLE [dbo].[HoaDon] CHECK CONSTRAINT [FK_HD_GG]
GO
ALTER TABLE [dbo].[HoaDon]  WITH CHECK ADD  CONSTRAINT [FK_HD_KH] FOREIGN KEY([MAKH])
REFERENCES [dbo].[KhachHang] ([MAKH])
GO
ALTER TABLE [dbo].[HoaDon] CHECK CONSTRAINT [FK_HD_KH]
GO
ALTER TABLE [dbo].[HoaDon]  WITH CHECK ADD  CONSTRAINT [FK_HD_NV] FOREIGN KEY([MANV])
REFERENCES [dbo].[NhanVien] ([MANV])
GO
ALTER TABLE [dbo].[HoaDon] CHECK CONSTRAINT [FK_HD_NV]
GO
ALTER TABLE [dbo].[KhachHang]  WITH CHECK ADD  CONSTRAINT [FK_KH_TK] FOREIGN KEY([MATAIKHOAN])
REFERENCES [dbo].[TaiKhoan] ([MATAIKHOAN])
GO
ALTER TABLE [dbo].[KhachHang] CHECK CONSTRAINT [FK_KH_TK]
GO
ALTER TABLE [dbo].[NguyenLieu]  WITH CHECK ADD  CONSTRAINT [FK_NL_KHO] FOREIGN KEY([MAKHO])
REFERENCES [dbo].[Kho] ([MAKHO])
GO
ALTER TABLE [dbo].[NguyenLieu] CHECK CONSTRAINT [FK_NL_KHO]
GO
ALTER TABLE [dbo].[NhanVien]  WITH CHECK ADD  CONSTRAINT [FK_NV_TK] FOREIGN KEY([MATAIKHOAN])
REFERENCES [dbo].[TaiKhoan] ([MATAIKHOAN])
GO
ALTER TABLE [dbo].[NhanVien] CHECK CONSTRAINT [FK_NV_TK]
GO
ALTER TABLE [dbo].[PhieuNhapKho]  WITH CHECK ADD  CONSTRAINT [FK_NK_NV] FOREIGN KEY([MANV])
REFERENCES [dbo].[NhanVien] ([MANV])
GO
ALTER TABLE [dbo].[PhieuNhapKho] CHECK CONSTRAINT [FK_NK_NV]
GO
ALTER TABLE [dbo].[PhieuXuatKho]  WITH CHECK ADD  CONSTRAINT [FK_XK_NV] FOREIGN KEY([MANV])
REFERENCES [dbo].[NhanVien] ([MANV])
GO
ALTER TABLE [dbo].[PhieuXuatKho] CHECK CONSTRAINT [FK_XK_NV]
GO
ALTER TABLE [dbo].[TaiKhoan]  WITH CHECK ADD  CONSTRAINT [FK_TK_QTC] FOREIGN KEY([MAQUYEN])
REFERENCES [dbo].[QuyenTruyCap] ([MAQUYEN])
GO
ALTER TABLE [dbo].[TaiKhoan] CHECK CONSTRAINT [FK_TK_QTC]
GO

/* =====================================================================
   DỮ LIỆU MẪU
   ===================================================================== */

-- Quyền: Q001 là khách hàng (code đăng ký ghi cứng Q001)
INSERT INTO dbo.QuyenTruyCap (MAQUYEN, TENQUYEN) VALUES
('Q001', N'Khách hàng'),
('Q002', N'Quản lý'),
('Q003', N'Nhân viên');
GO

-- Tài khoản đã xác thực email (ISEMAILCONFIRMED = 1) để đăng nhập được ngay
INSERT INTO dbo.TaiKhoan (MATAIKHOAN, MAQUYEN, TENDANGNHAP, MATKHAU, EMAIL, ISEMAILCONFIRMED) VALUES
('TK0001', 'Q002', 'admin',     '123456', 'admin@freedom.vn',     1),
('TK0002', 'Q003', 'nhanvien',  '123456', 'nhanvien@freedom.vn',  1),
('TK0003', 'Q001', 'khachhang', '123456', 'khachhang@freedom.vn', 1);
GO

INSERT INTO dbo.NhanVien (MANV, MATAIKHOAN, HOTEN, CHUCVU, SODT, LUONG, SOGIOLAM) VALUES
('NV0001', 'TK0001', N'Nguyễn Văn Quản', N'Quản lý',   '0901000001', 15000000, 0),
('NV0002', 'TK0002', N'Trần Thị Phục Vụ', N'Phục vụ', '0901000002',  7000000, 0);
GO

INSERT INTO dbo.KhachHang (MAKH, MATAIKHOAN, HOTEN, NGAYSINH, SODT, DIEMTHANHVIEN, AVATAR) VALUES
('KH0001', 'TK0003', N'Lê Minh Khách', '2004-05-20', '0912000001', 0, NULL);
GO

INSERT INTO dbo.Ban (MABAN, TRANGTHAI) VALUES
('B01', N'Trống'), ('B02', N'Trống'), ('B03', N'Trống'), ('B04', N'Trống'), ('B05', N'Trống'),
('B06', N'Trống'), ('B07', N'Trống'), ('B08', N'Trống'), ('B09', N'Trống'), ('B10', N'Trống');
GO

-- Món ăn: tên/mô tả khớp đúng nội dung từng ảnh trong wwwroot/Images/MonAn (TENMON tối đa 20 ký tự)
INSERT INTO dbo.MonAn (MAMONAN, TENMON, LOAIMON, GIA, AnhMon, MOTA) VALUES
('MA0001', N'Gỏi cuốn chay',        N'Khai vị',    45000, 'goi_cuon_chay.jpg',               N'Bánh tráng cuốn rau sống, bún, đậu hũ, chấm tương đậu phộng.'),
('MA0002', N'Gỏi xoài chay',        N'Khai vị',    55000, 'goi-xoai-chay.jpg',               N'Xoài xanh trộn rau thơm, cà rốt và đậu phộng rang.'),
('MA0003', N'Salad rau củ',         N'Khai vị',    65000, 'salad_rau_cu.jpg',                N'Xà lách, cà chua, dưa leo, ô liu và phô mai, sốt dầu giấm.'),
('MA0004', N'Chả bắp chiên',        N'Khai vị',    45000, 'cha-bap.jpg',                     N'Bắp ngọt trộn bột chiên vàng giòn, chấm tương ớt.'),
('MA0005', N'Nấm bào ngư chiên',    N'Khai vị',    65000, 'nam-bao-ngu-chien.jpg',           N'Nấm bào ngư tẩm bột chiên giòn, chấm tương ớt.'),
('MA0006', N'Dưa chuột muối chay',  N'Khai vị',    30000, 'dua-chuot-muoi-chay.jpg',         N'Dưa chuột muối chua ngọt cùng tỏi và ớt, ăn kèm cơm.'),
('MA0007', N'Bắp cải cuộn chay',    N'Món chính',  65000, 'bone.jpg',                        N'Lá bắp cải cuộn nhân nấm, đậu hũ và rau củ, hấp mềm thanh ngọt.'),
('MA0008', N'Cà tím nướng sốt cay', N'Món chính',  60000, 'ca-tim-xao-chay.jpg',             N'Cà tím nướng mềm, rưới sốt ớt tỏi và hành lá thơm.'),
('MA0009', N'Đậu hũ sốt cà chua',   N'Món chính',  50000, 'dau_hu_sot_ca.jpg',               N'Đậu hũ chiên sốt cà chua đậm đà, rắc hành lá.'),
('MA0010', N'Đậu hũ non hấp nấm',   N'Món chính',  60000, 'dau-phu-non-hap-nam.png',         N'Đậu hũ non hấp chín mềm, phủ nấm kim châm sốt nước tương.'),
('MA0011', N'Đậu cô ve xào',        N'Món xào',    45000, 'dau-co-ve-xao-vung.jpg',          N'Đậu cô ve xào giòn, rắc hành phi thơm.'),
('MA0012', N'Đậu hũ xào măng tươi', N'Món xào',    55000, 'dau-hu-xao-mang-tuoi.jpg',        N'Măng tươi xào đậu hũ và nấm, vị ngọt giòn tự nhiên.'),
('MA0013', N'Khổ qua xào đậu hũ',   N'Món xào',    50000, 'kho-qua-xao-dau-hu-ca-rot.jpg',   N'Khổ qua xào đậu hũ, cà rốt, bắp và đậu Hà Lan.'),
('MA0014', N'Khoai tây xào',        N'Món xào',    45000, 'khoai-tay-xao.jpg',               N'Khoai tây thái lát xào thì là, mềm bùi.'),
('MA0015', N'Nấm đùi gà xào',       N'Món xào',    65000, 'nam-dui-ga-xao.jpg',              N'Nấm đùi gà xào rau thơm, dai ngọt đậm vị.'),
('MA0016', N'Cải thảo xào cay',     N'Món xào',    45000, 'muchap.jpg',                      N'Cải thảo xào ớt khô, giòn ngọt, cay nhẹ.'),
('MA0017', N'Canh chua chay',       N'Canh - Súp', 50000, 'canhchua.jpg',                    N'Canh chua bạc hà, đậu bắp, cà chua, thơm và nấm, vị chua dịu.'),
('MA0018', N'Canh nấm cà chua',     N'Canh - Súp', 45000, 'canh-nam-ca-chua.jpg',            N'Nấm rơm, đậu hũ non nấu cùng cà chua, nước dùng rau củ ngọt thanh.'),
('MA0019', N'Canh nấm hạt sen',     N'Canh - Súp', 60000, 'canh-nam-hat-sen.jpg',            N'Nấm đông cô, hạt sen, đậu hũ, táo đỏ và rau củ hầm bổ dưỡng.'),
('MA0020', N'Canh xà lách xoong',   N'Canh - Súp', 40000, 'canh-rau-xa-lach-xoong-chay.jpg', N'Xà lách xoong nấu nấm và đậu hũ, mát lành, dễ ăn.'),
('MA0021', N'Canh khoai mỡ tím',    N'Canh - Súp', 40000, 'coca.jpg',                        N'Khoai mỡ tím nấu sánh với rau ngò om, bùi và thơm.'),
('MA0022', N'Canh bí đỏ nấm',       N'Canh - Súp', 45000, 'ganuong.jpg',                     N'Bí đỏ nấu cùng nấm, nước dùng vàng sánh, ngọt bùi.'),
('MA0023', N'Cơm chiên Dương Châu', N'Cơm - Bún',  55000, 'com_chien_duong_chau.jpg',        N'Cơm chiên cùng đậu Hà Lan, cà rốt, bắp và chả chay.'),
('MA0024', N'Cơm tấm sườn chay',    N'Cơm - Bún',  60000, 'com_tam.jpg',                     N'Cơm tấm ăn kèm sườn chay nướng, chả trứng, đồ chua và nước mắm chay.'),
('MA0025', N'Bún riêu chay',        N'Cơm - Bún',  55000, 'comchien.jpg',                    N'Bún với riêu đậu hũ, cà chua, nấm, giá và rau thơm.'),
('MA0026', N'Bún chả chay',         N'Cơm - Bún',  55000, 'mixao.jpg',                       N'Bún ăn kèm chả chay nướng, rau sống và nước chấm chua ngọt.'),
('MA0027', N'Phở chay',             N'Cơm - Bún',  50000, 'pho_chay.jpg',                    N'Phở nước dùng rau củ, nấm, đậu hũ và rau thơm.'),
('MA0028', N'Mì Ý sốt cà chua',     N'Cơm - Bún',  70000, 'spaghetti_chay.jpg',              N'Mì Ý sốt cà chua và nấm băm, rắc lá húng tây.'),
('MA0029', N'Bánh mì chay',         N'Ăn nhẹ',     30000, 'lauthai.jpg',                     N'Bánh mì giòn kẹp chả chay, đồ chua, dưa leo và rau mùi.'),
('MA0030', N'Lẩu Thái chay',        N'Lẩu',       250000, 'lau_thai.jpg',                    N'Lẩu chua cay kiểu Thái với nấm, đậu hũ và rau củ tươi (2-3 người).');
GO

INSERT INTO dbo.Kho (MAKHO, SOLUONGTON) VALUES ('K01', 0);
GO

INSERT INTO dbo.NguyenLieu (MANGUYENLIEU, MAKHO, TENNGUYENLIEU, DONGIA, DVT, SoLuongTon) VALUES
('NL0001', 'K01', N'Đậu hũ',       15000, N'kg',  20),
('NL0002', 'K01', N'Nấm rơm',      60000, N'kg',  10),
('NL0003', 'K01', N'Nấm đùi gà',   70000, N'kg',  10),
('NL0004', 'K01', N'Cà chua',      20000, N'kg',  15),
('NL0005', 'K01', N'Bún tươi',     15000, N'kg',  20),
('NL0006', 'K01', N'Gạo',          18000, N'kg',  50),
('NL0007', 'K01', N'Rau thơm',     30000, N'kg',   5),
('NL0008', 'K01', N'Bánh tráng',   40000, N'xấp', 10);
GO

INSERT INTO dbo.MaGiamGia (MaGiamGia_Code, NgayBD, NgayKT, SoLuong, SoTien) VALUES
('FREEDOM10', '2026-01-01', '2027-12-31', 100, 10000),
('FREEDOM50', '2026-01-01', '2027-12-31',  20, 50000);
GO

-- Đánh giá mẫu (tài khoản khachhang)
INSERT INTO dbo.DanhGia (MADANHGIA, MATAIKHOAN, LOAIDANHGIA, MAMONAN, MANV, SOSAO, DANHGIA, NGAYDANHGIA) VALUES
('DG0001', 'TK0003', N'NhaHang',  NULL,     NULL,     5, N'Không gian yên tĩnh, sạch sẽ, món chay rất vừa miệng. Sẽ quay lại!', '2026-10-20T19:30:00'),
('DG0002', 'TK0003', N'MonAn',    'MA0030', NULL,     4, N'Lẩu Thái chay chua cay vừa phải, nấm tươi. Nên thêm chút rau.',   '2026-10-25T12:10:00'),
('DG0003', 'TK0003', N'NhanVien', NULL,     'NV0002', 5, N'Bạn phục vụ nhiệt tình, tư vấn món rất kỹ.',                        '2026-10-28T18:45:00');
GO
