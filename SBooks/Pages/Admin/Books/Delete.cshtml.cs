using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Books
{
    public class DeleteModel : AdminPageModel
    {
        public DeleteModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty]
        public Book Book { get; set; } = default!;

        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Admin)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            Book = book;
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
                var book = await _context.Books
                    .Include(b => b.Reviews)
                    .Include(b => b.FavoriteBooks)
                    .FirstOrDefaultAsync(m => m.BookId == id);

                if (book == null)
                {
                    return NotFound();
                }

                // Kiểm tra ràng buộc dữ liệu
                var hasReviews = book.Reviews.Any();
                var hasFavorites = book.FavoriteBooks.Any();

                if (hasReviews || hasFavorites)
                {
                    // Load lại dữ liệu để hiển thị
                    Book = await _context.Books
                        .Include(b => b.Author)
                        .Include(b => b.Publisher)
                        .Include(b => b.Admin)
                        .FirstOrDefaultAsync(m => m.BookId == id) ?? book;

                    var issues = new List<string>();
                    if (hasReviews)
                        issues.Add($"{book.Reviews.Count} đánh giá/bình luận");
                    if (hasFavorites)
                        issues.Add($"{book.FavoriteBooks.Count} lượt yêu thích");

                    ErrorMessage = $"Không thể xóa sách này vì còn tồn tại: {string.Join(", ", issues)}. " +
                                 "Vui lòng xóa các dữ liệu liên quan trước khi xóa sách.";
                    return Page();
                }

                // Xóa ảnh nếu có
                if (!string.IsNullOrEmpty(book.UrlImage))
                {
                    DeleteImage(book.UrlImage);
                }

                // Xóa sách
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa sách '{book.Title}' thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                // Load lại dữ liệu nếu có lỗi
                Book = await _context.Books
                    .Include(b => b.Author)
                    .Include(b => b.Publisher)
                    .Include(b => b.Admin)
                    .FirstOrDefaultAsync(m => m.BookId == id) ?? new Book();

                ErrorMessage = $"Có lỗi xảy ra khi xóa sách: {ex.Message}";
                return Page();
            }
        }

        private void DeleteImage(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/images/books/"))
                    return;

                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "books", fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception)
            {
                // Log the error but don't throw - we don't want to fail the deletion because of image deletion
            }
        }
    }
}