using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVÝS KAYITLARI ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Veritabaný baðlamý
builder.Services.AddDbContext<LibraryContext>();

// CORS Ayarý - Tarayýcý hatalarýný önlemek için þart
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// --- VERÝTABANI TABLOLARINI OTOMATÝK OLUÞTURMA ---
// Bu blok uygulama her baþladýðýnda tablolarý kontrol eder ve yoksa oluþturur.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    context.Database.EnsureCreated();
}

// --- 2. MIDDLEWARE SIRALAMASI (ÇOK KRÝTÝK) ---
// Sýralama yanlýþ olduðunda 404 veya HTML hatalarý alýnýr.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Render üzerinde HTTPS yönlendirmesi bazen port çakýþmasý yapar, 
// Render zaten SSL saðladýðý için bunu kapalý tutmak daha güvenlidir.
// app.UseHttpsRedirection(); 

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html"); // Yollar karýþýrsa index.html'e geri döner

app.Run();

// --- MODELLER ---
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime? ReturnDate { get; set; }
}

// --- VERÝTABANI BAÐLAMI ---
public class LibraryContext : DbContext
{
    public DbSet<Book> Books { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder o)
    {
        var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (string.IsNullOrEmpty(connUrl))
        {
            // Yerel çalýþma (SQL Server)
            o.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=LibraryDB;Trusted_Connection=True;");
        }
        else
        {
            // Neon.tech URI formatýný (postgresql://...) Npgsql formatýna çeviriyoruz
            var databaseUri = new Uri(connUrl);
            var userInfo = databaseUri.UserInfo.Split(':');

            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Require,
                TrustServerCertificate = true,
                IncludeErrorDetail = true
            };

            o.UseNpgsql(builder.ConnectionString);
        }
    }
}