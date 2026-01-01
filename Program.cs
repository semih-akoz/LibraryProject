using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVÝS KAYITLARI ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Veritabaný baðlamý
builder.Services.AddDbContext<LibraryContext>();

// CORS Ayarý
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// --- VERÝTABANI TABLOLARINI OTOMATÝK OLUÞTURMA ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    try
    {
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Veritabaný oluþturma hatasý: " + ex.Message);
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

// --- VERÝTABANI BAÐLAMI ---
public class LibraryContext : DbContext
{
    public DbSet<Book> Books { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder o)
    {
        var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (string.IsNullOrEmpty(connUrl))
        {
            // Yerel SQL Server
            o.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=LibraryDB;Trusted_Connection=True;");
        }
        else
        {
            // Render/Neon PostgreSQL - En sade ve güvenli baðlantý yöntemi
            // Eðer baþýnda 'psql ' veya týrnak varsa onlarý temizle
            var cleanString = connUrl.Replace("psql ", "").Replace("'", "").Trim();

            o.UseNpgsql(cleanString, npgsqlOptions => {
                npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
        }
    }
}