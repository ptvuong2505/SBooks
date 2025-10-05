
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using SBooks.Pages.Admin;

namespace SBooks.Pages.Admin.Dashboard
{
    public class IndexModel : AdminPageModel
    {
        public IndexModel(SbooksContext context) : base(context)
        {
        }

        // Thống kê tổng quan
        public int TotalBooks { get; set; }
        public int TotalAuthors { get; set; }
        public int TotalUsers { get; set; }
        public int TotalPublishers { get; set; }
        public int TotalReviews { get; set; }
        public int TotalFavorites { get; set; }

        // Hoạt động gần đây
        public List<User> RecentUsers { get; set; } = new();
        public List<Book> RecentBooks { get; set; } = new();
        public List<Review> RecentReviews { get; set; } = new();

        // Top items
        public List<Book> TopViewedBooks { get; set; } = new();
        public List<string> TopSearchTerms { get; set; } = new();
        public List<Author> TopAuthors { get; set; } = new();

        // Thống kê theo thể loại
        public List<GenreStats> GenreStatistics { get; set; } = new();

        // Hoạt động theo tháng
        public List<MonthlyStats> MonthlyStatistics { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Thống kê tổng quan
                TotalBooks = await _context.Books.CountAsync();
                TotalAuthors = await _context.Authors.CountAsync();
                TotalUsers = await _context.Users.CountAsync();
                TotalPublishers = await _context.Publishers.CountAsync();
                TotalReviews = await _context.Reviews.CountAsync();
                TotalFavorites = await _context.FavoriteBooks.CountAsync();

                // Người dùng mới nhất (5 người)
                RecentUsers = await _context.Users.Include(u => u.Roles)
                    .Where(u => u.Roles.Any(r => r.RoleName == "User"))
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Sách mới nhất (5 cuốn)
                RecentBooks = await _context.Books
                    .Include(b => b.Author)
                    .Include(b => b.Publisher)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Đánh giá mới nhất (5 đánh giá)
                RecentReviews = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Book)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Top 5 sách được xem nhiều nhất
                TopViewedBooks = await _context.Books
                    .Include(b => b.Author)
                    .Where(b => b.ViewCount.HasValue)
                    .OrderByDescending(b => b.ViewCount)
                    .Take(5)
                    .ToListAsync();

                // Top 5 từ khóa tìm kiếm phổ biến
                var allSearchTerms = await _context.SearchLogs
                    .Where(s => !string.IsNullOrEmpty(s.SearchText))
                    .Select(s => s.SearchText)
                    .ToListAsync();

                TopSearchTerms = allSearchTerms
                    .GroupBy(s => s)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key ?? "")
                    .ToList();

                // Top 5 tác giả có nhiều sách nhất
                var authorsWithBookCount = await _context.Authors
                    .Select(a => new
                    {
                        Author = a,
                        BookCount = a.Books.Count()
                    })
                    .OrderByDescending(x => x.BookCount)
                    .Take(5)
                    .ToListAsync();

                TopAuthors = authorsWithBookCount.Select(x => x.Author).ToList();

                // Thống kê theo thể loại
                var allBooks = await _context.Books
                    .Where(b => !string.IsNullOrEmpty(b.Genre))
                    .Select(b => new { b.Genre, b.ViewCount })
                    .ToListAsync();

                GenreStatistics = allBooks
                    .GroupBy(b => b.Genre)
                    .Select(g => new GenreStats
                    {
                        Genre = g.Key!,
                        BookCount = g.Count(),
                        TotalViews = g.Sum(b => b.ViewCount ?? 0)
                    })
                    .OrderByDescending(g => g.BookCount)
                    .Take(10)
                    .ToList();

                // Thống kê 6 tháng gần đây
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);
                var booksInTimeRange = await _context.Books
                    .Where(b => b.CreatedAt.HasValue && b.CreatedAt >= sixMonthsAgo)
                    .Select(b => new { b.CreatedAt })
                    .ToListAsync();

                MonthlyStatistics = booksInTimeRange
                    .GroupBy(b => new
                    {
                        Year = b.CreatedAt!.Value.Year,
                        Month = b.CreatedAt.Value.Month
                    })
                    .Select(g => new MonthlyStats
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        BookCount = g.Count()
                    })
                    .OrderBy(m => m.Year).ThenBy(m => m.Month)
                    .ToList();
            }
            catch (Exception ex)
            {
                // Log error (you can add logging here)
                System.Diagnostics.Debug.WriteLine($"Dashboard error: {ex.Message}");

                // Initialize empty collections to prevent view errors
                RecentUsers = new List<User>();
                RecentBooks = new List<Book>();
                RecentReviews = new List<Review>();
                TopViewedBooks = new List<Book>();
                TopSearchTerms = new List<string>();
                TopAuthors = new List<Author>();
                GenreStatistics = new List<GenreStats>();
                MonthlyStatistics = new List<MonthlyStats>();
            }
        }
    }

    // Class helper cho thống kê
    public class GenreStats
    {
        public string Genre { get; set; } = "";
        public int BookCount { get; set; }
        public int TotalViews { get; set; }
    }

    public class MonthlyStats
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int BookCount { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MM/yyyy");
    }
}