using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using QuanLyNhaHang_.Models;
using QuanLyNhaHang_.Services;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký kết nối cơ sở dữ liệu
builder.Services.AddDbContext<DbQlNhaHangContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("connect1")));

// Đăng ký các lớp service nghiệp vụ
builder.Services.AddScoped<XuLyDatBan>();
builder.Services.AddScoped<XuLyGiamGia>();
builder.Services.AddScoped<XuLyHoaDon>();
builder.Services.AddScoped<XuLyNguyenLieu>();
builder.Services.AddScoped<XuLyPhieuGoiMon>();
builder.Services.AddScoped<XuLyThanhToan>();
builder.Services.AddScoped<XuLyThongTinKhachHang>();
builder.Services.AddScoped<XuLyThongTinMonAn>();
builder.Services.AddScoped<XuLyThongTinNhanVien>();
builder.Services.AddScoped<XuLyXuatKho>();
builder.Services.AddScoped<XuLyMaGiamGia>();
builder.Services.AddScoped<XuLyKet>();
builder.Services.AddScoped<XuLyQuy>();
// Session (AccountController dùng HttpContext.Session)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Cấu hình middleware
app.UseStaticFiles();

// Map thủ công các thư mục tĩnh nằm ở gốc project (assets, assets_admin và Images)
foreach (var folder in new[] { "assets", "assets_admin", "Images" })
{
    var path = Path.Combine(builder.Environment.ContentRootPath, folder);
    if (Directory.Exists(path))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(path),
            RequestPath = "/" + folder
        });
    }
}

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Welcome}/{id?}");

app.Run();