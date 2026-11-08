-- =====================================================================
-- NÂNG CẤP CSDL (đợt sửa 6): trạng thái phiếu đặt bàn + khách của phiếu gọi món.
-- Dùng cho máy ĐÃ CÓ database DB_QL_NhaHang: mở file trong SSMS -> Execute (F5).
-- Chạy nhiều lần cũng không lỗi (chỉ thêm khi chưa có).
-- Máy cài mới thì chạy DB_QL_NhaHang.sql là đủ (đã gồm các cột này).
-- (Nếu chưa chạy NangCap_DanhGia_Ban.sql thì chạy file đó TRƯỚC.)
-- =====================================================================
USE [DB_QL_NhaHang]
GO

/* ---------- PhieuDatBan: trạng thái + bàn đã xếp ---------- */
IF COL_LENGTH('dbo.PhieuDatBan', 'TRANGTHAI') IS NULL
    ALTER TABLE dbo.PhieuDatBan ADD TRANGTHAI nvarchar(20) NULL;   -- Chờ xếp bàn | Đang phục vụ | Đã phục vụ | Đã hủy
IF COL_LENGTH('dbo.PhieuDatBan', 'MABAN') IS NULL
    ALTER TABLE dbo.PhieuDatBan ADD MABAN varchar(10) NULL;
GO

/* ---------- PhieuGoiMon: khách / tài khoản của phiếu ---------- */
IF COL_LENGTH('dbo.PhieuGoiMon', 'MAKH') IS NULL
    ALTER TABLE dbo.PhieuGoiMon ADD MAKH char(10) NULL;
IF COL_LENGTH('dbo.PhieuGoiMon', 'TENKHACH') IS NULL
    ALTER TABLE dbo.PhieuGoiMon ADD TENKHACH nvarchar(50) NULL;
IF COL_LENGTH('dbo.PhieuGoiMon', 'SDTKHACH') IS NULL
    ALTER TABLE dbo.PhieuGoiMon ADD SDTKHACH varchar(11) NULL;
GO
IF OBJECT_ID('dbo.FK_PGM_KH', 'F') IS NULL
    ALTER TABLE dbo.PhieuGoiMon WITH CHECK ADD CONSTRAINT FK_PGM_KH FOREIGN KEY (MAKH) REFERENCES dbo.KhachHang (MAKH);
GO

/* ---------- Cập nhật dữ liệu cũ ---------- */
-- Phiếu gọi món đang mở: lấy khách đang ngồi ở bàn làm khách của phiếu
UPDATE p SET p.MAKH = b.MAKH, p.TENKHACH = b.TENKHACH, p.SDTKHACH = b.SDTKHACH
FROM dbo.PhieuGoiMon p
JOIN dbo.Ban b ON b.MABAN = p.MABAN
WHERE p.MAKH IS NULL AND p.TENKHACH IS NULL;

-- Khách đặt trước đang ngồi ở bàn -> "Đang phục vụ"
UPDATE pd SET pd.TRANGTHAI = N'Đang phục vụ', pd.MABAN = RTRIM(b.MABAN)
FROM dbo.PhieuDatBan pd
JOIN dbo.Ban b ON b.MAKH = pd.MAKH AND b.TRANGTHAI = N'Đầy'
WHERE pd.TRANGTHAI IS NULL AND pd.MAKH IS NOT NULL;

-- Phiếu đặt của những ngày đã qua -> "Đã phục vụ" (lịch sử, không còn nằm trong danh sách chờ)
UPDATE dbo.PhieuDatBan SET TRANGTHAI = N'Đã phục vụ'
WHERE TRANGTHAI IS NULL AND NGAYDAT < CAST(GETDATE() AS date);

-- Còn lại -> chờ xếp bàn
UPDATE dbo.PhieuDatBan SET TRANGTHAI = N'Chờ xếp bàn' WHERE TRANGTHAI IS NULL;
GO

PRINT N'Nâng cấp xong: PhieuDatBan (TRANGTHAI, MABAN) và PhieuGoiMon (MAKH, TENKHACH, SDTKHACH).';
GO
