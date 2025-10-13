using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using SBooks.Models.DTOs;
using System.Security.Claims;

namespace SBooks.Services
{
    public interface IBookService
    {
        Task<PaginatedResult<BookCardDto>> GetBooksAsync(SearchFilterDto filter, long? currentUserId = null);
        Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit = 10);
        Task<List<Author>> GetAuthorsAsync();
        Task<List<string>> GetGenresAsync();
        Task LogSearchAsync(string searchText, long? userId);
        Task<List<BookCardDto>> GetTopFavoriteBooksAsync(int limit = 10, long? currentUserId = null);
    }

    public class BookService : IBookService
    {
        private readonly SbooksContext _context;

        public BookService(SbooksContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<BookCardDto>> GetBooksAsync(SearchFilterDto filter, long? currentUserId = null)
        {
            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Reviews)
                .Include(b => b.FavoriteBooks)
                .AsQueryable();

            // Tìm kiếm theo title và author name
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var searchLower = filter.SearchQuery.ToLower();
                query = query.Where(b =>
                    b.Title.ToLower().Contains(searchLower) ||
                    (b.Author != null && b.Author.AuthorName.ToLower().Contains(searchLower)));
            }

            // Lọc theo author
            if (filter.AuthorIds.Any())
            {
                query = query.Where(b => b.AuthorId.HasValue && filter.AuthorIds.Contains(b.AuthorId.Value));
            }

            // Lọc theo genre
            if (filter.Genres.Any())
            {
                query = query.Where(b => !string.IsNullOrEmpty(b.Genre) && filter.Genres.Contains(b.Genre));
            }

            // Đếm tổng số
            var totalCount = await query.CountAsync();

            // Sắp xếp
            query = filter.SortBy?.ToLower() switch
            {
                "oldest" => query.OrderBy(b => b.CreatedAt),
                "title" => query.OrderBy(b => b.Title),
                "rating" => query.OrderByDescending(b => b.Reviews
                    .Where(r => r.ParentReviewId == null && r.Rating.HasValue)
                    .Average(r => (double?)r.Rating) ?? 0),
                "favorites" => query.OrderByDescending(b => b.FavoriteBooks.Count),
                _ => query.OrderByDescending(b => b.CreatedAt) // newest (default)
            };

            // Phân trang
            var books = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new BookCardDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Description = b.Description,
                    PublishedYear = b.PublishedYear,
                    Genre = b.Genre,
                    Price = b.Price,
                    UrlImage = b.UrlImage,
                    AuthorName = b.Author != null ? b.Author.AuthorName : null,
                    PublisherName = b.Publisher != null ? b.Publisher.PublisherName : null,
                    AverageRating = b.Reviews
                        .Where(r => r.ParentReviewId == null && r.Rating.HasValue)
                        .Average(r => (double?)r.Rating) ?? 0,
                    ReviewCount = b.Reviews.Count(r => r.ParentReviewId == null),
                    FavoriteCount = b.FavoriteBooks.Count,
                    IsFavorited = currentUserId.HasValue &&
                        b.FavoriteBooks.Any(f => f.UserId == currentUserId.Value)
                })
                .ToListAsync();

            return new PaginatedResult<BookCardDto>
            {
                Items = books,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<SearchSuggestionDto>();

            var searchLower = query.ToLower();

            return await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Reviews)
                .Where(b =>
                    b.Title.ToLower().Contains(searchLower) ||
                    (b.Author != null && b.Author.AuthorName.ToLower().Contains(searchLower)))
                .OrderByDescending(b => b.ViewCount)
                .Take(limit)
                .Select(b => new SearchSuggestionDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    AuthorName = b.Author != null ? b.Author.AuthorName : null,
                    UrlImage = b.UrlImage,
                    AverageRating = b.Reviews
                        .Where(r => r.ParentReviewId == null && r.Rating.HasValue)
                        .Average(r => (double?)r.Rating) ?? 0
                })
                .ToListAsync();
        }

        public async Task<List<Author>> GetAuthorsAsync()
        {
            return await _context.Authors
                .Where(a => a.Books.Any()) // Chỉ lấy author có sách
                .OrderBy(a => a.AuthorName)
                .ToListAsync();
        }

        public async Task<List<string>> GetGenresAsync()
        {
            return await _context.Books
                .Where(b => !string.IsNullOrEmpty(b.Genre))
                .Select(b => b.Genre!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();
        }

        public async Task LogSearchAsync(string searchText, long? userId)
        {
            var searchLog = new SearchLog
            {
                SearchText = searchText,
                UserId = userId,
                SearchTime = DateTime.UtcNow
            };

            _context.SearchLogs.Add(searchLog);
            await _context.SaveChangesAsync();
        }

        public async Task<List<BookCardDto>> GetTopFavoriteBooksAsync(int limit = 10, long? currentUserId = null)
        {
            return await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Reviews)
                .Include(b => b.FavoriteBooks)
                .Where(b => b.FavoriteBooks.Any()) // Chỉ lấy sách có ít nhất 1 lượt yêu thích
                .OrderByDescending(b => b.FavoriteBooks.Count)
                .ThenByDescending(b => b.ViewCount)
                .Take(limit)
                .Select(b => new BookCardDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
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
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();
        }
    }
}