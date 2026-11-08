-- Nâng cấp CSDL: thêm bảng PhuPhi (phụ phí / bồi thường khi khách làm vỡ bát, hỏng đồ...).
-- Chạy 1 lần trong SSMS trên CSDL đang dùng (DB_QL_NhaHang). Cài mới hoàn toàn thì chạy xong DB_QL_NhaHang.sql rồi chạy file này.
USE [DB_QL_NhaHang]
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PhuPhi')
BEGIN
    CREATE TABLE [dbo].[PhuPhi](
        [MAPHUPHI] [int] IDENTITY(1,1) NOT NULL,
        [MAPGM] [varchar](10) NOT NULL,
        [MABAN] [varchar](10) NULL,
        [NOIDUNG] [nvarchar](200) NOT NULL,
        [SOTIEN] [int] NOT NULL,
        [MANV] [char](10) NULL,
        [THOIGIAN] [datetime] NOT NULL,
        [MAHOADON] [char](10) NULL,
        PRIMARY KEY CLUSTERED ([MAPHUPHI] ASC)
            WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
                  ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    PRINT 'Đã tạo bảng PhuPhi.'
END
ELSE
    PRINT 'Bảng PhuPhi đã có sẵn, không tạo lại.'
GO
