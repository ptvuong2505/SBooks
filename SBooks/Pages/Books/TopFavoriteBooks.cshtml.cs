
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using SBooks.Models.DTOs;
using SBooks.Extensions;

namespace SBooks.Pages.Books
{
    public class TopFavoriteBooksModel : PageModel
    {
        private readonly SbooksContext _context;

        public TopFavoriteBooksModel(SbooksContext context)
        {
            _context = context;
        }

        public List<TopFavoriteBookDto> TopFavoriteBooks { get; set; } = new();
        public int TotalBooks { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.GetUserId();

            // Get top favorite books with all necessary information
            var topBooks = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Reviews)
                .Include(b => b.FavoriteBooks)
                .Where(b => b.FavoriteBooks.Any()) // Only books that have at least one favorite
                .Select(b => new TopFavoriteBookDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Description = b.Description,
                    PublishedYear = b.PublishedYear,
                    Genre = b.Genre,
                    Price = b.Price,
                    UrlImage = b.UrlImage,
                    AuthorId = b.Author != null ? b.Author.AuthorId : null,
                    AuthorName = b.Author != null ? b.Author.AuthorName : null,
                    PublisherName = b.Publisher != null ? b.Publisher.PublisherName : null,
                    FavoriteCount = b.FavoriteBooks.Count,
                    AverageRating = b.Reviews
                        .Where(r => r.ParentReviewId == null && r.Rating.HasValue)
                        .Average(r => (double?)r.Rating) ?? 0,
                    ReviewCount = b.Reviews.Count(r => r.ParentReviewId == null),
                    ViewCount = b.ViewCount ?? 0,
                    IsFavorited = userId.HasValue && b.FavoriteBooks.Any(f => f.UserId == userId.Value)
                })
                .OrderByDescending(b => b.FavoriteCount)
                .ThenByDescending(b => b.AverageRating)
                .Take(50) // Top 50 most favorited books
                .ToListAsync();

            // Add ranking
            for (int i = 0; i < topBooks.Count; i++)
            {
                topBooks[i].Rank = i + 1;
            }

            TopFavoriteBooks = topBooks;
            TotalBooks = await _context.Books.CountAsync(b => b.FavoriteBooks.Any());

            return Page();
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(long bookId)
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
                _context.FavoriteBooks.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, isFavorited = false });
            }
            else
            {
                var favorite = new FavoriteBook
                {
                    BookId = bookId,
                    UserId = userId.Value
                };

                _context.FavoriteBooks.Add(favorite);
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, isFavorited = true });
            }
        }
    }
}

