using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using SBooks.Models.DTOs;

namespace SBooks.Services
{
    public interface IAuthorService
    {
        Task<Author?> GetAuthorByIdAsync(long authorId);
        Task<PaginatedResult<BookCardDto>> GetBooksByAuthorAsync(long authorId, int page = 1, int pageSize = 12, long? currentUserId = null);
        Task<List<Author>> GetAllAuthorsAsync(int page = 1, int pageSize = 20);
        Task<int> GetTotalAuthorsCountAsync();
        Task<AuthorStatsDto> GetAuthorStatsAsync(long authorId);
    }

    public class AuthorService : IAuthorService
    {
        private readonly SbooksContext _context;
        private readonly ILogger<AuthorService> _logger;

        public AuthorService(SbooksContext context, ILogger<AuthorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Author?> GetAuthorByIdAsync(long authorId)
        {
            return await _context.Authors
                .FirstOrDefaultAsync(a => a.AuthorId == authorId);
        }

        public async Task<PaginatedResult<BookCardDto>> GetBooksByAuthorAsync(long authorId, int page = 1, int pageSize = 12, long? currentUserId = null)
        {
            var query = _context.Books
                .Where(b => b.AuthorId == authorId)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Reviews)
                .Include(b => b.FavoriteBooks);

            var totalCount = await query.CountAsync();

            var books = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookCardDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Description = b.Description,
                    AuthorName = b.Author != null ? b.Author.AuthorName : "Không rõ",
                    AuthorId = b.AuthorId,
                    PublisherName = b.Publisher != null ? b.Publisher.PublisherName : "Không rõ",
                    Genre = b.Genre,
                    Price = b.Price,
                    UrlImage = b.UrlImage,
                    ViewCount = b.ViewCount ?? 0,
                    AverageRating = b.Reviews.Where(r => r.Rating.HasValue && r.ParentReviewId == null)
                        .Average(r => (double?)r.Rating) ?? 0,
                    ReviewCount = b.Reviews.Count(r => r.ParentReviewId == null),
                    FavoriteCount = b.FavoriteBooks.Count,
                    IsFavorited = currentUserId.HasValue && b.FavoriteBooks.Any(fb => fb.UserId == currentUserId.Value),
                    CreatedAt = b.CreatedAt,
                    PublishedYear = b.PublishedYear
                })
                .ToListAsync();

            return new PaginatedResult<BookCardDto>
            {
                Items = books,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<Author>> GetAllAuthorsAsync(int page = 1, int pageSize = 20)
        {
            return await _context.Authors
                .OrderBy(a => a.AuthorName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalAuthorsCountAsync()
        {
            return await _context.Authors.CountAsync();
        }

        public async Task<AuthorStatsDto> GetAuthorStatsAsync(long authorId)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                    .ThenInclude(b => b.Reviews)
                .Include(a => a.Books)
                    .ThenInclude(b => b.FavoriteBooks)
                .FirstOrDefaultAsync(a => a.AuthorId == authorId);

            if (author == null)
                return new AuthorStatsDto();

            var totalBooks = author.Books.Count;
            var totalViews = author.Books.Sum(b => b.ViewCount ?? 0);
            var totalFavorites = author.Books.Sum(b => b.FavoriteBooks.Count);
            var totalReviews = author.Books.Sum(b => b.Reviews.Count(r => r.ParentReviewId == null));
            var averageRating = author.Books
                .Where(b => b.Reviews.Any(r => r.Rating.HasValue && r.ParentReviewId == null))
                .Select(b => b.Reviews.Where(r => r.Rating.HasValue && r.ParentReviewId == null).Average(r => (double)r.Rating!.Value))
                .DefaultIfEmpty(0)
                .Average();

            var genres = author.Books
                .Where(b => !string.IsNullOrEmpty(b.Genre))
                .GroupBy(b => b.Genre)
                .Select(g => new { Genre = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToDictionary(g => g.Genre!, g => g.Count);

            return new AuthorStatsDto
            {
                TotalBooks = totalBooks,
                TotalViews = totalViews,
                TotalFavorites = totalFavorites,
                TotalReviews = totalReviews,
                AverageRating = averageRating,
                TopGenres = genres,
                MostPopularBook = author.Books
                    .OrderByDescending(b => b.FavoriteBooks.Count)
                    .ThenByDescending(b => b.ViewCount)
                    .FirstOrDefault()
            };
        }
    }

    public class AuthorStatsDto
    {
        public int TotalBooks { get; set; }
        public int TotalViews { get; set; }
        public int TotalFavorites { get; set; }
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public Dictionary<string, int> TopGenres { get; set; } = new();
        public Book? MostPopularBook { get; set; }
    }
}