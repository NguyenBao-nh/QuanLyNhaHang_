using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QuanLyNhaHang_.Models;

public partial class DbQlNhaHangContext : DbContext
{
    public DbQlNhaHangContext()
    {
    }

    public DbQlNhaHangContext(DbContextOptions<DbQlNhaHangContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Ban> Bans { get; set; }

    public virtual DbSet<ChiTietGoiMon> ChiTietGoiMons { get; set; }

    public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    public virtual DbSet<ChiTietNhapKho> ChiTietNhapKhos { get; set; }

    public virtual DbSet<ChiTietXuatKho> ChiTietXuatKhos { get; set; }

    public virtual DbSet<DanhGia> DanhGia { get; set; }

    public virtual DbSet<GiamGia> GiamGia { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<Kho> Khos { get; set; }

    public virtual DbSet<MonAn> MonAns { get; set; }

    public virtual DbSet<NguyenLieu> NguyenLieus { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<PhieuDatBan> PhieuDatBans { get; set; }

    public virtual DbSet<PhieuGoiMon> PhieuGoiMons { get; set; }

    public virtual DbSet<PhieuNhapKho> PhieuNhapKhos { get; set; }

    public virtual DbSet<PhieuXuatKho> PhieuXuatKhos { get; set; }

    public virtual DbSet<QuyenTruyCap> QuyenTruyCaps { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<MaGiamGia> MaGiamGia { get; set; } = null!;

    public virtual DbSet<HuyMon> HuyMons { get; set; } = null!;

    public virtual DbSet<CaLamViec> CaLamViecs { get; set; } = null!;

    public virtual DbSet<GiaoDichKet> GiaoDichKets { get; set; } = null!;

    public virtual DbSet<PhuPhi> PhuPhis { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=DB_QL_NhaHang;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<Ban>(entity =>
        {
            entity.HasKey(e => e.Maban).HasName("PK__Ban__4033B61AF893BBF2");
            entity.ToTable("Ban");
            entity.Property(e => e.Maban)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MABAN");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(10)
                .HasColumnName("TRANGTHAI");

            // Khách đang ngồi ở bàn (xem Database/NangCap_DanhGia_Ban.sql)
            entity.Property(e => e.Makh)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAKH");
            entity.Property(e => e.Tenkhach)
                .HasMaxLength(50)
                .HasColumnName("TENKHACH");
            entity.Property(e => e.Sdtkhach)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("SDTKHACH");
            entity.Property(e => e.Giovao)
                .HasColumnType("datetime")
                .HasColumnName("GIOVAO");

            entity.HasOne(d => d.MakhNavigation).WithMany()
                .HasForeignKey(d => d.Makh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BAN_KH");
        });

        modelBuilder.Entity<ChiTietGoiMon>(entity =>
        {
            entity.HasKey(e => new { e.Mapgm, e.Mamonan }).HasName("PK__ChiTietG__65939448BDB2499F");
            entity.ToTable("ChiTietGoiMon");
            entity.Property(e => e.Mapgm)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MAPGM");
            entity.Property(e => e.Mamonan)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MAMONAN");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
        });

        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.HasKey(e => new { e.Mahoadon, e.Mamonan }).HasName("PK__ChiTietH__BA3F290457209A19");
            entity.ToTable("ChiTietHoaDon");
            entity.Property(e => e.Mahoadon)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAHOADON");
            entity.Property(e => e.Mamonan)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAMONAN");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Tghoanthanh)
                .HasColumnType("datetime")
                .HasColumnName("TGHOANTHANH");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");

            entity.HasOne(d => d.MahoadonNavigation).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.Mahoadon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHD_HD");

            entity.HasOne(d => d.MamonanNavigation).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.Mamonan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHD_MA");
        });

        modelBuilder.Entity<ChiTietNhapKho>(entity =>
        {
            entity.HasKey(e => new { e.Manhapkho, e.Manguyenlieu }).HasName("PK__ChiTietN__71CDECBC2B82A61D");
            entity.ToTable("ChiTietNhapKho");
            entity.Property(e => e.Manhapkho)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANHAPKHO");
            entity.Property(e => e.Manguyenlieu)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANGUYENLIEU");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");

            entity.HasOne(d => d.ManguyenlieuNavigation).WithMany(p => p.ChiTietNhapKhos)
                .HasForeignKey(d => d.Manguyenlieu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTNK_NL");

            entity.HasOne(d => d.ManhapkhoNavigation).WithMany(p => p.ChiTietNhapKhos)
                .HasForeignKey(d => d.Manhapkho)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTNK_NK");
        });

        modelBuilder.Entity<ChiTietXuatKho>(entity =>
        {
            entity.HasKey(e => new { e.Maxuatkho, e.Manguyenlieu }).HasName("PK__ChiTietX__FC5F95647A8724C0");
            entity.ToTable("ChiTietXuatKho");
            entity.Property(e => e.Maxuatkho)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAXUATKHO");
            entity.Property(e => e.Manguyenlieu)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANGUYENLIEU");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");

            entity.HasOne(d => d.ManguyenlieuNavigation).WithMany(p => p.ChiTietXuatKhos)
                .HasForeignKey(d => d.Manguyenlieu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTXK_NL");

            entity.HasOne(d => d.MaxuatkhoNavigation).WithMany(p => p.ChiTietXuatKhos)
                .HasForeignKey(d => d.Maxuatkho)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTXK_XK");
        });

        modelBuilder.Entity<DanhGia>(entity =>
        {
            entity.HasKey(e => e.Madanhgia).HasName("PK__DanhGia__8597D60AC6DB265D");
            entity.Property(e => e.Madanhgia)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MADANHGIA");
            entity.Property(e => e.Danhgia)
                .HasMaxLength(1000)
                .HasColumnName("DANHGIA");
            entity.Property(e => e.Mahoadon)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAHOADON");

            entity.HasOne(d => d.MahoadonNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.Mahoadon)
                .HasConstraintName("FK_DG_HD");

            // Các cột mới cho mục Đánh giá (xem Database/NangCap_DanhGia_Ban.sql)
            entity.Property(e => e.Mataikhoan)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MATAIKHOAN");
            entity.Property(e => e.Loaidanhgia)
                .HasMaxLength(20)
                .HasColumnName("LOAIDANHGIA");
            entity.Property(e => e.Mamonan)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAMONAN");
            entity.Property(e => e.Manv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANV");
            entity.Property(e => e.Sosao).HasColumnName("SOSAO");
            entity.Property(e => e.Hinhanh)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("HINHANH");
            entity.Property(e => e.Ngaydanhgia)
                .HasColumnType("datetime")
                .HasColumnName("NGAYDANHGIA");
            // Phản hồi của nhà hàng (Database/NangCap_Dot7_DonTrucTuyen_HuyMon.sql)
            entity.Property(e => e.Phanhoi)
                .HasMaxLength(1000)
                .HasColumnName("PHANHOI");
            entity.Property(e => e.Ngayphanhoi)
                .HasColumnType("datetime")
                .HasColumnName("NGAYPHANHOI");
            entity.Property(e => e.Manvphanhoi)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANVPHANHOI");

            entity.HasOne(d => d.MataikhoanNavigation).WithMany()
                .HasForeignKey(d => d.Mataikhoan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DG_TK");
            entity.HasOne(d => d.MamonanNavigation).WithMany()
                .HasForeignKey(d => d.Mamonan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DG_MA");
            entity.HasOne(d => d.ManvNavigation).WithMany()
                .HasForeignKey(d => d.Manv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DG_NV");
        });

        modelBuilder.Entity<GiamGia>(entity =>
        {
            entity.HasKey(e => e.Magiamgia).HasName("PK__GiamGia__41C2843981C7A11C");
            entity.Property(e => e.Magiamgia)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAGIAMGIA");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CODE");
            entity.Property(e => e.Ngaybd)
                .HasColumnType("datetime")
                .HasColumnName("NGAYBD");
            entity.Property(e => e.Ngaykt)
                .HasColumnType("datetime")
                .HasColumnName("NGAYKT");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.Mahoadon).HasName("PK__HoaDon__A4999DF52123A1D6");
            entity.ToTable("HoaDon");
            entity.Property(e => e.Mahoadon)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAHOADON");
            entity.Property(e => e.Hinhthuc)
                .HasMaxLength(20)
                .HasColumnName("HINHTHUC");
            // Cột mới (Database/NangCap_HoaDon_KhachVangLai.sql)
            entity.Property(e => e.Tenkhach)
                .HasMaxLength(50)
                .HasColumnName("TENKHACH");
            entity.Property(e => e.Sdtkhach)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("SDTKHACH");
            entity.Ignore(e => e.TenHienThi);
            entity.Ignore(e => e.LaDonTrucTuyen);
            // Đơn trực tuyến (Database/NangCap_Dot7_DonTrucTuyen_HuyMon.sql)
            entity.Property(e => e.Loaidon).HasMaxLength(20).HasColumnName("LOAIDON");
            entity.Property(e => e.Trangthai).HasMaxLength(30).HasColumnName("TRANGTHAI");
            entity.Property(e => e.Diachigiao).HasMaxLength(200).HasColumnName("DIACHIGIAO");
            entity.Property(e => e.Ghichu).HasMaxLength(300).HasColumnName("GHICHU");
            entity.Property(e => e.Tgxacnhan).HasColumnType("datetime").HasColumnName("TGXACNHAN");
            entity.Property(e => e.Tggiao).HasColumnType("datetime").HasColumnName("TGGIAO");
            entity.Property(e => e.Lydohuy).HasMaxLength(200).HasColumnName("LYDOHUY");
            // Thanh toán trước của đơn trực tuyến (Database/NangCap_Dot8_ThanhToanTruoc_Quy.sql)
            entity.Property(e => e.Anhchuyenkhoan).HasMaxLength(200).IsUnicode(false).HasColumnName("ANHCHUYENKHOAN");
            entity.Property(e => e.Magiaodich).HasMaxLength(50).HasColumnName("MAGIAODICH");
            entity.Property(e => e.Maca).HasMaxLength(10).IsUnicode(false).HasColumnName("MACA");
            entity.Property(e => e.Maban)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MABAN");
            entity.Property(e => e.Magiamgia)
                .HasMaxLength(50)
                .HasColumnName("MAGIAMGIA");
            entity.Property(e => e.Makh)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAKH");
            entity.Property(e => e.Manv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANV");
            entity.Property(e => e.Ngaylap)
                .HasColumnType("datetime")
                .HasColumnName("NGAYLAP");
            entity.Property(e => e.Tongtien).HasColumnName("TONGTIEN");

            entity.HasOne(d => d.MabanNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.Maban)
                .HasConstraintName("FK_HD_BAN");

            entity.HasOne(d => d.MakhNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.Makh)
                .HasConstraintName("FK_HD_KH");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.Manv)
                .HasConstraintName("FK_HD_NV");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.Makh).HasName("PK__KhachHan__603F592C8562D6A1");
            entity.ToTable("KhachHang");
            entity.Property(e => e.Makh)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAKH");
            entity.Property(e => e.Avatar)
                .HasMaxLength(1500)
                .IsUnicode(false)
                .HasColumnName("AVATAR");
            entity.Property(e => e.Diemthanhvien).HasColumnName("DIEMTHANHVIEN");
            entity.Property(e => e.Hoten)
                .HasMaxLength(20)
                .HasColumnName("HOTEN");
            entity.Property(e => e.Mataikhoan)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MATAIKHOAN");
            entity.Property(e => e.Ngaysinh).HasColumnName("NGAYSINH");
            entity.Property(e => e.Sodt)
                .HasMaxLength(11)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("SODT");

            entity.HasOne(d => d.MataikhoanNavigation).WithMany(p => p.KhachHangs)
                .HasForeignKey(d => d.Mataikhoan)
                .HasConstraintName("FK_KH_TK");
        });

        modelBuilder.Entity<Kho>(entity =>
        {
            entity.HasKey(e => e.Makho).HasName("PK__Kho__7AFB3D16BE142BA7");
            entity.ToTable("Kho");
            entity.Property(e => e.Makho)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAKHO");
            entity.Property(e => e.Soluongton).HasColumnName("SOLUONGTON");
        });

        modelBuilder.Entity<MonAn>(entity =>
        {
            entity.HasKey(e => e.Mamonan).HasName("PK__MonAn__EA6B4F1065EBAC6A");
            entity.ToTable("MonAn");
            entity.Property(e => e.Mamonan)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAMONAN");
            entity.Property(e => e.AnhMon)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Gia).HasColumnName("GIA");
            entity.Property(e => e.Loaimon)
                .HasMaxLength(20)
                .HasColumnName("LOAIMON");
            entity.Property(e => e.Tenmon)
                .HasMaxLength(20)
                .HasColumnName("TENMON");
        });

        modelBuilder.Entity<NguyenLieu>(entity =>
        {
            entity.HasKey(e => e.Manguyenlieu).HasName("PK__NguyenLi__9BD3E01D1CBE81A4");
            entity.ToTable("NguyenLieu");
            entity.Property(e => e.Manguyenlieu)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANGUYENLIEU");
            entity.Property(e => e.Dongia).HasColumnName("DONGIA");
            entity.Property(e => e.Dvt)
                .HasMaxLength(10)
                .HasColumnName("DVT");
            entity.Property(e => e.Makho)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAKHO");
            entity.Property(e => e.Tennguyenlieu)
                .HasMaxLength(30)
                .HasColumnName("TENNGUYENLIEU");

            entity.HasOne(d => d.MakhoNavigation).WithMany(p => p.NguyenLieus)
                .HasForeignKey(d => d.Makho)
                .HasConstraintName("FK_NL_KHO");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.Manv).HasName("PK__NhanVien__603F511440967C2E");
            entity.ToTable("NhanVien");
            entity.Property(e => e.Manv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANV");
            entity.Property(e => e.Chucvu)
                .HasMaxLength(20)
                .HasColumnName("CHUCVU");
            entity.Property(e => e.Hoten)
                .HasMaxLength(20)
                .HasColumnName("HOTEN");
            entity.Property(e => e.Luong).HasColumnName("LUONG");
            entity.Property(e => e.Mataikhoan)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MATAIKHOAN");
            entity.Property(e => e.Sodt)
                .HasMaxLength(11)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("SODT");
            entity.Property(e => e.Sogiolam).HasColumnName("SOGIOLAM");

            entity.HasOne(d => d.MataikhoanNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.Mataikhoan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NV_TK");
        });

        modelBuilder.Entity<PhieuDatBan>(entity =>
        {
            entity.HasKey(e => e.Maphieu).HasName("PK__PhieuDat__F001B94191A89524");
            entity.ToTable("PhieuDatBan");
            entity.Property(e => e.Maphieu)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MAPHIEU");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Giodat)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("GIODAT");
            entity.Property(e => e.Makh)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MAKH");
            entity.Property(e => e.Ngaydat)
                .HasColumnType("datetime")
                .HasColumnName("NGAYDAT");
            entity.Property(e => e.Sdt)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("SDT");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Tenkh)
                .HasMaxLength(50)
                .HasColumnName("TENKH");
            // Cột mới (Database/NangCap_DatBan_PhieuGoiMon.sql)
            entity.Property(e => e.Trangthai)
                .HasMaxLength(20)
                .HasColumnName("TRANGTHAI");
            entity.Property(e => e.Maban)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MABAN");
        });

        modelBuilder.Entity<PhieuGoiMon>(entity =>
        {
            entity.HasKey(e => e.Mapgm).HasName("PK__PhieuGoi__7B3520B9662B974E");
            entity.ToTable("PhieuGoiMon");
            entity.Property(e => e.Mapgm)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MAPGM");
            entity.Property(e => e.Maban)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MABAN");
            entity.Property(e => e.Manv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MANV");
            entity.Property(e => e.Ngaylap)
                .HasColumnType("datetime")
                .HasColumnName("NGAYLAP");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(20)
                .HasColumnName("TRANGTHAI");

            // Cột mới (Database/NangCap_DatBan_PhieuGoiMon.sql)
            entity.Property(e => e.Makh)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAKH");
            entity.Property(e => e.Tenkhach)
                .HasMaxLength(50)
                .HasColumnName("TENKHACH");
            entity.Property(e => e.Sdtkhach)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("SDTKHACH");
            entity.HasOne(d => d.MakhNavigation).WithMany()
                .HasForeignKey(d => d.Makh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PGM_KH");
        });

        modelBuilder.Entity<PhieuNhapKho>(entity =>
        {
            entity.HasKey(e => e.Manhapkho).HasName("PK__PhieuNha__B870D2BD49D621AA");
            entity.ToTable("PhieuNhapKho");
            entity.Property(e => e.Manhapkho)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANHAPKHO");
            entity.Property(e => e.Manv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANV");
            entity.Property(e => e.Ngaynk)
                .HasColumnType("datetime")
                .HasColumnName("NGAYNK");
            entity.Property(e => e.Tongtien).HasColumnName("TONGTIEN");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.PhieuNhapKhos)
                .HasForeignKey(d => d.Manv)
                .HasConstraintName("FK_NK_NV");
        });

        modelBuilder.Entity<PhieuXuatKho>(entity =>
        {
            entity.HasKey(e => e.Maxuatkho).HasName("PK__PhieuXua__35E2AB65DC07BA73");
            entity.ToTable("PhieuXuatKho");
            entity.Property(e => e.Maxuatkho)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAXUATKHO");
            entity.Property(e => e.Manv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANV");
            entity.Property(e => e.Ngayxk)
                .HasColumnType("datetime")
                .HasColumnName("NGAYXK");
            entity.Property(e => e.Tongtien).HasColumnName("TONGTIEN");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.PhieuXuatKhos)
                .HasForeignKey(d => d.Manv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_XK_NV");
        });

        modelBuilder.Entity<QuyenTruyCap>(entity =>
        {
            entity.HasKey(e => e.Maquyen).HasName("PK__QuyenTru__F2A840CF5E29DFFC");
            entity.ToTable("QuyenTruyCap");
            entity.Property(e => e.Maquyen)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAQUYEN");
            entity.Property(e => e.Tenquyen)
                .HasMaxLength(15)
                .HasColumnName("TENQUYEN");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.Mataikhoan).HasName("PK__TaiKhoan__2ED8B517929E106C");
            entity.ToTable("TaiKhoan");
            entity.Property(e => e.Mataikhoan)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MATAIKHOAN");
            entity.Property(e => e.Email)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Emailconfirmationtoken)
                .HasMaxLength(100)
                .HasColumnName("EMAILCONFIRMATIONTOKEN");
            entity.Property(e => e.Isemailconfirmed)
                .HasDefaultValue(0)
                .HasColumnName("ISEMAILCONFIRMED");
            entity.Property(e => e.Maquyen)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAQUYEN");
            entity.Property(e => e.Matkhau)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("MATKHAU");
            entity.Property(e => e.Otp).HasColumnName("OTP");
            entity.Property(e => e.Tendangnhap)
                .HasMaxLength(20)
                .HasColumnName("TENDANGNHAP");

            entity.HasOne(d => d.MaquyenNavigation).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.Maquyen)
                .HasConstraintName("FK_TK_QTC");
        });

        modelBuilder.Entity<MaGiamGia>(entity =>
        {
            entity.HasKey(e => e.MaGiamGia_Code);
            entity.ToTable("MaGiamGia");
            entity.Property(e => e.MaGiamGia_Code)
                .HasMaxLength(50)
                .HasColumnName("MaGiamGia_Code");
            entity.Property(e => e.PhanTram).HasColumnName("PhanTram");
            entity.Ignore(e => e.MoTa);
        });

        // Ca làm việc + két (Database/NangCap_Dot9_CaLamViec_Ket.sql)
        modelBuilder.Entity<CaLamViec>(entity =>
        {
            entity.HasKey(e => e.Maca);
            entity.ToTable("CaLamViec");
            entity.Ignore(e => e.DangMo);
            entity.Property(e => e.Maca).HasMaxLength(10).IsUnicode(false).HasColumnName("MACA");
            entity.Property(e => e.Loaica).HasMaxLength(10).HasColumnName("LOAICA");
            entity.Property(e => e.Ngay).HasColumnType("date").HasColumnName("NGAY");
            entity.Property(e => e.Trangthai).HasMaxLength(20).HasColumnName("TRANGTHAI");
            entity.Property(e => e.Manvmo).HasMaxLength(10).IsUnicode(false).IsFixedLength().HasColumnName("MANVMO");
            entity.Property(e => e.Tennvmo).HasMaxLength(50).HasColumnName("TENNVMO");
            entity.Property(e => e.Tgmo).HasColumnType("datetime").HasColumnName("TGMO");
            entity.Property(e => e.Tiendauca).HasColumnName("TIENDAUCA");
            entity.Property(e => e.Manvdong).HasMaxLength(10).IsUnicode(false).IsFixedLength().HasColumnName("MANVDONG");
            entity.Property(e => e.Tennvdong).HasMaxLength(50).HasColumnName("TENNVDONG");
            entity.Property(e => e.Tgdong).HasColumnType("datetime").HasColumnName("TGDONG");
            entity.Property(e => e.Sohoadon).HasColumnName("SOHOADON");
            entity.Property(e => e.Doanhthu).HasColumnName("DOANHTHU");
            entity.Property(e => e.Thutienmat).HasColumnName("THUTIENMAT");
            entity.Property(e => e.Thuchuyenkhoan).HasColumnName("THUCHUYENKHOAN");
            entity.Property(e => e.Tongchi).HasColumnName("TONGCHI");
            entity.Property(e => e.Tongrut).HasColumnName("TONGRUT");
            entity.Property(e => e.Tienketcuoi).HasColumnName("TIENKETCUOI");
            entity.Property(e => e.Tienthucte).HasColumnName("TIENTHUCTE");
            entity.Property(e => e.Ghichu).HasMaxLength(300).HasColumnName("GHICHU");
        });

        modelBuilder.Entity<GiaoDichKet>(entity =>
        {
            entity.HasKey(e => e.Magd);
            entity.ToTable("GiaoDichKet");
            entity.Ignore(e => e.LaTienVao);
            entity.Property(e => e.Magd).HasColumnName("MAGD").ValueGeneratedOnAdd();
            entity.Property(e => e.Maca).HasMaxLength(10).IsUnicode(false).HasColumnName("MACA");
            entity.Property(e => e.Loai).HasMaxLength(20).HasColumnName("LOAI");
            entity.Property(e => e.Sotien).HasColumnName("SOTIEN");
            entity.Property(e => e.Noidung).HasMaxLength(300).HasColumnName("NOIDUNG");
            entity.Property(e => e.Machungtu).HasMaxLength(20).IsUnicode(false).HasColumnName("MACHUNGTU");
            entity.Property(e => e.Thoigian).HasColumnType("datetime").HasColumnName("THOIGIAN");
            entity.Property(e => e.Manv).HasMaxLength(10).IsUnicode(false).IsFixedLength().HasColumnName("MANV");
        });

        modelBuilder.Entity<HuyMon>(entity =>
        {
            entity.HasKey(e => e.Mahuy);
            entity.ToTable("HuyMon");
            entity.Property(e => e.Mahuy).HasColumnName("MAHUY").ValueGeneratedOnAdd();
            entity.Property(e => e.Mapgm).HasMaxLength(10).IsUnicode(false).HasColumnName("MAPGM");
            entity.Property(e => e.Maban).HasMaxLength(10).IsUnicode(false).HasColumnName("MABAN");
            entity.Property(e => e.Mamonan).HasMaxLength(10).IsUnicode(false).IsFixedLength().HasColumnName("MAMONAN");
            entity.Property(e => e.Tenmon).HasMaxLength(50).HasColumnName("TENMON");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Lydo).HasMaxLength(200).HasColumnName("LYDO");
            entity.Property(e => e.Manv).HasMaxLength(10).IsUnicode(false).IsFixedLength().HasColumnName("MANV");
            entity.Property(e => e.Thoigian).HasColumnType("datetime").HasColumnName("THOIGIAN");
        });

        // Phụ phí / bồi thường (Database/NangCap_PhuPhiBoiThuong.sql)
        modelBuilder.Entity<PhuPhi>(entity =>
        {
            entity.HasKey(e => e.Maphuphi);
            entity.ToTable("PhuPhi");
            entity.Property(e => e.Maphuphi).HasColumnName("MAPHUPHI").ValueGeneratedOnAdd();
            entity.Property(e => e.Mapgm).HasMaxLength(10).IsUnicode(false).HasColumnName("MAPGM");
            entity.Property(e => e.Maban).HasMaxLength(10).IsUnicode(false).HasColumnName("MABAN");
            entity.Property(e => e.Noidung).HasMaxLength(200).HasColumnName("NOIDUNG");
            entity.Property(e => e.Sotien).HasColumnName("SOTIEN");
            entity.Property(e => e.Manv).HasMaxLength(10).IsUnicode(false).IsFixedLength().HasColumnName("MANV");
            entity.Property(e => e.Thoigian).HasColumnType("datetime").HasColumnName("THOIGIAN");
            entity.Property(e => e.Mahoadon).HasMaxLength(10).IsUnicode(false).IsFixedLength().HasColumnName("MAHOADON");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}