-- =====================================================================
-- NÂNG CẤP CSDL cho bản cập nhật: Đánh giá (sao, ảnh, nhà hàng/món/nhân viên)
-- và gắn khách hàng / tài khoản vào bàn.
-- Dùng cho máy ĐÃ CÓ database DB_QL_NhaHang: mở file trong SSMS -> Execute (F5).
-- Chạy nhiều lần cũng không lỗi (chỉ thêm khi chưa có).
-- Máy cài mới thì chạy DB_QL_NhaHang.sql là đủ (đã gồm các cột này).
-- =====================================================================
USE [DB_QL_NhaHang]
GO

/* ---------- Bảng Ban: khách đang ngồi ---------- */
IF COL_LENGTH('dbo.Ban', 'MAKH') IS NULL
    ALTER TABLE dbo.Ban ADD MAKH char(10) NULL;
IF COL_LENGTH('dbo.Ban', 'TENKHACH') IS NULL
    ALTER TABLE dbo.Ban ADD TENKHACH nvarchar(50) NULL;
IF COL_LENGTH('dbo.Ban', 'SDTKHACH') IS NULL
    ALTER TABLE dbo.Ban ADD SDTKHACH varchar(11) NULL;
IF COL_LENGTH('dbo.Ban', 'GIOVAO') IS NULL
    ALTER TABLE dbo.Ban ADD GIOVAO datetime NULL;
GO
IF OBJECT_ID('dbo.FK_BAN_KH', 'F') IS NULL
    ALTER TABLE dbo.Ban WITH CHECK ADD CONSTRAINT FK_BAN_KH FOREIGN KEY (MAKH) REFERENCES dbo.KhachHang (MAKH);
GO

/* ---------- Bảng DanhGia: đánh giá công khai ---------- */
IF COL_LENGTH('dbo.DanhGia', 'MATAIKHOAN') IS NULL
    ALTER TABLE dbo.DanhGia ADD MATAIKHOAN char(10) NULL;
IF COL_LENGTH('dbo.DanhGia', 'LOAIDANHGIA') IS NULL
    ALTER TABLE dbo.DanhGia ADD LOAIDANHGIA nvarchar(20) NULL;      -- NhaHang | MonAn | NhanVien
IF COL_LENGTH('dbo.DanhGia', 'MAMONAN') IS NULL
    ALTER TABLE dbo.DanhGia ADD MAMONAN char(10) NULL;
IF COL_LENGTH('dbo.DanhGia', 'MANV') IS NULL
    ALTER TABLE dbo.DanhGia ADD MANV char(10) NULL;
IF COL_LENGTH('dbo.DanhGia', 'SOSAO') IS NULL
    ALTER TABLE dbo.DanhGia ADD SOSAO int NULL;
IF COL_LENGTH('dbo.DanhGia', 'HINHANH') IS NULL
    ALTER TABLE dbo.DanhGia ADD HINHANH varchar(200) NULL;
IF COL_LENGTH('dbo.DanhGia', 'NGAYDANHGIA') IS NULL
    ALTER TABLE dbo.DanhGia ADD NGAYDANHGIA datetime NULL;
GO
IF OBJECT_ID('dbo.CK_DG_SOSAO', 'C') IS NULL
    ALTER TABLE dbo.DanhGia ADD CONSTRAINT CK_DG_SOSAO CHECK (SOSAO IS NULL OR SOSAO BETWEEN 1 AND 5);
IF OBJECT_ID('dbo.FK_DG_TK', 'F') IS NULL
    ALTER TABLE dbo.DanhGia WITH CHECK ADD CONSTRAINT FK_DG_TK FOREIGN KEY (MATAIKHOAN) REFERENCES dbo.TaiKhoan (MATAIKHOAN);
IF OBJECT_ID('dbo.FK_DG_MA', 'F') IS NULL
    ALTER TABLE dbo.DanhGia WITH CHECK ADD CONSTRAINT FK_DG_MA FOREIGN KEY (MAMONAN) REFERENCES dbo.MonAn (MAMONAN);
IF OBJECT_ID('dbo.FK_DG_NV', 'F') IS NULL
    ALTER TABLE dbo.DanhGia WITH CHECK ADD CONSTRAINT FK_DG_NV FOREIGN KEY (MANV) REFERENCES dbo.NhanVien (MANV);
GO

PRINT N'Nâng cấp xong: bảng Ban (MAKH, TENKHACH, SDTKHACH, GIOVAO) và DanhGia (MATAIKHOAN, LOAIDANHGIA, MAMONAN, MANV, SOSAO, HINHANH, NGAYDANHGIA).';
GO
