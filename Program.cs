using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVÝS KAYITLARI ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Veritabaný baðlamýný (Context) kaydediyoruz
builder.Services.AddDbContext<LibraryContext>();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// --- 2. MIDDLEWARE AYARLARI ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles(); // index.html'i varsayýlan sayfa yapar
app.UseStaticFiles();  // wwwroot içindeki dosyalarýn okunmasýný saðlar
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();

// --- MODELLER VE VERÝTABANI BAÐLAMI ---
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }

    public string Author { get; set; } // YENÝ: Yazar ismi
    public bool IsAvailable { get; set; } = true;
    public DateTime? ReturnDate { get; set; } // Kitabýn geleceði tarih
}

//public class LibraryContext : DbContext
//{
//    public DbSet<Book> Books { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder o) =>
//        o.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=LibraryDB;Trusted_Connection=True;");
//}

public class LibraryContext : DbContext
{
    public DbSet<Book> Books { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder o)
    {
        // Render.com'a ekleyeceðimiz DATABASE_URL deðiþkenini kontrol eder
        var connString = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (string.IsNullOrEmpty(connString))
        {
            // YERELDEYSEN: Senin SQL Server'ýný kullanýr
            o.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=LibraryDB;Trusted_Connection=True;");
        }
        else
        {
            // BULUTTAYSAN: Neon.tech PostgreSQL'ini kullanýr
            o.UseNpgsql(connString);
        }
    }
}