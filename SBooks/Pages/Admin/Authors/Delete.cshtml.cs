using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Authors
{
    public class DeleteModel : AdminPageModel
    {
        public DeleteModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty]
        public Author Author { get; set; } = default!;

        public string ErrorMessage { get; set; } = string.Empty;
        public int BookCount { get; set; }
        public List<Book> AuthorBooks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                .Include(a => a.Books)
                    .ThenInclude(b => b.Publisher)
                .FirstOrDefaultAsync(m => m.AuthorId == id);

            if (author == null)
            {
                return NotFound();
            }

            Author = author;
            BookCount = author.Books.Count;
            AuthorBooks = author.Books.OrderByDescending(b => b.CreatedAt).Take(5).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var author = await _context.Authors
                    .Include(a => a.Books)
                    .FirstOrDefaultAsync(m => m.AuthorId == id);

                if (author == null)
                {
                    return NotFound();
                }

                // Kiểm tra ràng buộc dữ liệu
                var hasBooks = author.Books.Any();

                if (hasBooks)
                {
                    // Load lại dữ liệu để hiển thị
                    Author = author;
                    BookCount = author.Books.Count;
                    AuthorBooks = author.Books.OrderByDescending(b => b.CreatedAt).Take(5).ToList();

                    ErrorMessage = $"Không thể xóa tác giả này vì còn tồn tại {author.Books.Count} cuốn sách liên quan. " +
                                 "Vui lòng xóa hoặc chuyển các sách sang tác giả khác trước khi xóa.";
                    return Page();
                }

                // Xóa tác giả
                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa tác giả '{author.AuthorName}' thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                // Load lại dữ liệu nếu có lỗi
                Author = await _context.Authors
                    .Include(a => a.Books)
                        .ThenInclude(b => b.Publisher)
                    .FirstOrDefaultAsync(m => m.AuthorId == id) ?? new Author();

                BookCount = Author.Books?.Count ?? 0;
                AuthorBooks = Author.Books?.OrderByDescending(b => b.CreatedAt).Take(5).ToList() ?? new List<Book>();

                ErrorMessage = $"Có lỗi xảy ra khi xóa tác giả: {ex.Message}";
                return Page();
            }
        }
    }
}