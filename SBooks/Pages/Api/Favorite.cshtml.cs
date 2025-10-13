using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Extensions;
using SBooks.Models;

namespace SBooks.Pages.Api
{
    public class FavoriteModel : PageModel
    {
        private readonly SbooksContext _context;

        public FavoriteModel(SbooksContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync(long bookId)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return new JsonResult(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này" });
            }

            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Không thể xác định người dùng" });
            }

            var existingFavorite = await _context.FavoriteBooks
                .FirstOrDefaultAsync(f => f.BookId == bookId && f.UserId == userId.Value);

            if (existingFavorite != null)
            {
                // Remove from favorites
                _context.FavoriteBooks.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, isFavorited = false });
            }
            else
            {
                // Add to favorites
                var favorite = new FavoriteBook
                {
                    BookId = bookId,
                    UserId = userId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FavoriteBooks.Add(favorite);
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, isFavorited = true });
            }
        }
    }
}