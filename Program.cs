using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVİS KAYITLARI ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Veritabanı bağlamı
builder.Services.AddDbContext<LibraryContext>();

// CORS Ayarı
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// --- VERİTABANI TABLOLARINI OTOMATİK OLUŞTURMA ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    try
    {
        // Uygulama başlarken tabloyu kontrol eder/oluşturur
        context.Database.EnsureCreated();
        Console.WriteLine("Veritabanı bağlantısı ve tablo kontrolü başarılı.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Veritabanı tablosu hatası: " + ex.Message);
    }
}

// --- 2. MIDDLEWARE AYARLARI ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

// --- MODELLER ---
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public DateTime? ReturnDate { get; set; }
}

// --- VERİTABANI BAĞLAMI ---
public class LibraryContext : DbContext
{
    public DbSet<Book> Books { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder o)
    {
        // Önce Render üzerindeki değişkeni kontrol et
        var envConnection = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrEmpty(envConnection))
        {
            // Render üzerindeysek (PostgreSQL)
            o.UseNpgsql(envConnection);
        }
        else
        {
            // Bilgisayarda (Local) çalışıyorsan SQL Server kullan
            o.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=LibraryDB;Trusted_Connection=True;");
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>().ToTable("Books");
        
        // Kolon isimlerini kodla birebir eşliyoruz
        modelBuilder.Entity<Book>().Property(b => b.IsAvailable).HasColumnName("IsAvailable");
        modelBuilder.Entity<Book>().Property(b => b.ReturnDate).HasColumnName("ReturnDate");
    }
}

