using Microsoft.AspNetCore.Mvc;

namespace LibraryProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly LibraryContext _db;

        public BooksController(LibraryContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult GetBooks([FromQuery] string? search)
        {
            var query = _db.Books.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search));
            }

            return Ok(query.ToList());
        }

        [HttpPost]
        public IActionResult AddBook([FromBody] Book book)
        {
            if (book == null) return BadRequest();
            _db.Books.Add(book);
            _db.SaveChanges();
            return Ok(book);
        }

        // ÖDÜNÇ ALMA
        [HttpPost("{id}/lend")]
        public IActionResult LendBook(int id)
        {
            var book = _db.Books.Find(id);
            if (book == null) return NotFound("Kitap bulunamadı.");
            if (!book.IsAvailable) return BadRequest("Kitap zaten ödünç verilmiş.");

            book.IsAvailable = false;
            // .UtcNow kullanarak PostgreSQL uyumluluk hatasını çözüyoruz
            book.ReturnDate = DateTime.UtcNow.AddDays(14);
            
            // _db.SaveChanges();
            // return Ok(new { message = $"{book.Title} ödünç alındı. İade tarihi: {book.ReturnDate}" });
            
            _db.SaveChanges(); 
            return Ok(new { 
                message = $"{book.Title} ödünç alındı.", 
                returnDate = book.ReturnDate?.ToString("dd.MM.yyyy")
            });
        }
        

        // İADE ETME (Yeni Eklenen)
        [HttpPost("{id}/return")]
        public IActionResult ReturnBook(int id)
        {
            var book = _db.Books.Find(id);
            if (book == null) return NotFound("Kitap bulunamadı.");

            book.IsAvailable = true;
            book.ReturnDate = null; // İade edildiği için tarih sıfırlanır
            _db.SaveChanges();
            return Ok(new { message = $"{book.Title} başarıyla iade edildi." });
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBook(int id)
        {
            var book = _db.Books.Find(id);
            if (book == null) return NotFound("Kitap bulunamadı.");

            _db.Books.Remove(book);
            _db.SaveChanges();
            return Ok();
        }
    }
}