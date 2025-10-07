using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using System.Text.Json;

namespace SBooks.Pages.Admin.Reports
{
    public class IndexModel : AdminPageModel
    {
        public IndexModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty(SupportsGet = true)]
        public string ReportType { get; set; } = "overview";

        [BindProperty(SupportsGet = true)]
        public string DateRange { get; set; } = "last30days";

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        // Overview Statistics
        public int TotalBooks { get; set; }
        public int TotalAuthors { get; set; }
        public int TotalPublishers { get; set; }
        public int TotalUsers { get; set; }
        public int TotalReviews { get; set; }
        public int BooksThisMonth { get; set; }
        public int NewUsersThisMonth { get; set; }

        // Chart Data
        public string BooksTimelineJson { get; set; } = "[]";
        public string BooksByGenreJson { get; set; } = "[]";
        public string BooksByYearJson { get; set; } = "[]";
        public string TopAuthorsJson { get; set; } = "[]";
        public string TopPublishersJson { get; set; } = "[]";
        public string UserActivityJson { get; set; } = "[]";
        public string ReviewsTimelineJson { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            await LoadOverviewStatistics();
            await LoadChartData();
        }

        private async Task LoadOverviewStatistics()
        {
            TotalBooks = await _context.Books.CountAsync();
            TotalAuthors = await _context.Authors.CountAsync();
            TotalPublishers = await _context.Publishers.CountAsync();
            TotalUsers = await _context.Users.CountAsync();
            TotalReviews = await _context.Reviews.CountAsync();

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            BooksThisMonth = await _context.Books
                .Where(b => b.CreatedAt.HasValue && b.CreatedAt >= startOfMonth)
                .CountAsync();

            NewUsersThisMonth = await _context.Users
                .Where(u => u.CreatedAt.HasValue && u.CreatedAt >= startOfMonth)
                .CountAsync();
        }

        private async Task LoadChartData()
        {
            var (startDate, endDate) = GetDateRange();

            // Books Timeline
            await LoadBooksTimeline(startDate, endDate);

            // Books by Genre
            await LoadBooksByGenre();

            // Books by Year
            await LoadBooksByYear();

            // Top Authors
            await LoadTopAuthors();

            // Top Publishers
            await LoadTopPublishers();

            // User Activity
            await LoadUserActivity(startDate, endDate);

            // Reviews Timeline
            await LoadReviewsTimeline(startDate, endDate);
        }

        private async Task LoadBooksTimeline(DateTime startDate, DateTime endDate)
        {
            var startDateUtc = startDate.ToUniversalTime();
            var endDateUtc = endDate.ToUniversalTime();
            var books = await _context.Books
                .Where(b => b.CreatedAt.HasValue && b.CreatedAt >= startDateUtc && b.CreatedAt <= endDateUtc)
                .ToListAsync();

            var groupedBooks = books
                .GroupBy(b => b.CreatedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            var chartData = groupedBooks.Select(x => new
            {
                x = x.Date.ToString("yyyy-MM-dd"),
                y = x.Count
            }).ToList();

            BooksTimelineJson = JsonSerializer.Serialize(chartData);
        }

        private async Task LoadBooksByGenre()
        {
            var genreStats = await _context.Books
                .Where(b => !string.IsNullOrEmpty(b.Genre))
                .GroupBy(b => b.Genre)
                .Select(g => new { Genre = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            BooksByGenreJson = JsonSerializer.Serialize(genreStats);
        }

        private async Task LoadBooksByYear()
        {
            var yearStats = await _context.Books
                .Where(b => b.PublishedYear.HasValue)
                .GroupBy(b => b.PublishedYear)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .OrderBy(x => x.Year)
                .ToListAsync();

            BooksByYearJson = JsonSerializer.Serialize(yearStats);
        }

        private async Task LoadTopAuthors()
        {
            var authorStats = await _context.Authors
                .Include(a => a.Books)
                .Select(a => new
                {
                    Name = a.AuthorName,
                    BookCount = a.Books.Count,
                    Id = a.AuthorId
                })
                .Where(x => x.BookCount > 0)
                .OrderByDescending(x => x.BookCount)
                .Take(10)
                .ToListAsync();

            TopAuthorsJson = JsonSerializer.Serialize(authorStats);
        }

        private async Task LoadTopPublishers()
        {
            var publisherStats = await _context.Publishers
                .Include(p => p.Books)
                .Select(p => new
                {
                    Name = p.PublisherName,
                    BookCount = p.Books.Count,
                    Id = p.PublisherId
                })
                .Where(x => x.BookCount > 0)
                .OrderByDescending(x => x.BookCount)
                .Take(10)
                .ToListAsync();

            TopPublishersJson = JsonSerializer.Serialize(publisherStats);
        }

        private async Task LoadUserActivity(DateTime startDate, DateTime endDate)
        {
            var startDateUtc = startDate.ToUniversalTime();
            var endDateUtc = endDate.ToUniversalTime();
            var users = await _context.Users
                .Where(u => u.CreatedAt.HasValue && u.CreatedAt >= startDateUtc && u.CreatedAt <= endDateUtc)
                .ToListAsync();

            var userActivity = users
                .GroupBy(u => u.CreatedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            var chartData = userActivity.Select(x => new
            {
                x = x.Date.ToString("yyyy-MM-dd"),
                y = x.Count
            }).ToList();

            UserActivityJson = JsonSerializer.Serialize(chartData);
        }

        private async Task LoadReviewsTimeline(DateTime startDate, DateTime endDate)
        {
            var startDateUtc = startDate.ToUniversalTime();
            var endDateUtc = endDate.ToUniversalTime();
            var reviews = await _context.Reviews
                .Where(r => r.CreatedAt.HasValue && r.CreatedAt >= startDateUtc && r.CreatedAt <= endDateUtc)
                .ToListAsync();

            var groupedReviews = reviews
                .GroupBy(r => r.CreatedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            var chartData = groupedReviews.Select(x => new
            {
                x = x.Date.ToString("yyyy-MM-dd"),
                y = x.Count
            }).ToList();

            ReviewsTimelineJson = JsonSerializer.Serialize(chartData);
        }

        private (DateTime StartDate, DateTime EndDate) GetDateRange()
        {
            if (StartDate.HasValue && EndDate.HasValue)
            {
                return (StartDate.Value, EndDate.Value);
            }

            var endDate = DateTime.Now;
            var startDate = DateRange switch
            {
                "last7days" => endDate.AddDays(-7),
                "last30days" => endDate.AddDays(-30),
                "last90days" => endDate.AddDays(-90),
                "last6months" => endDate.AddMonths(-6),
                "lastyear" => endDate.AddYears(-1),
                "thisyear" => new DateTime(endDate.Year, 1, 1),
                "thismonth" => new DateTime(endDate.Year, endDate.Month, 1),
                _ => endDate.AddDays(-30)
            };

            return (startDate, endDate);
        }

        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            // Load data for PDF
            await LoadOverviewStatistics();

            var reportData = new
            {
                GeneratedAt = DateTime.Now,
                DateRange = GetDateRange(),
                Statistics = new
                {
                    TotalBooks,
                    TotalAuthors,
                    TotalPublishers,
                    TotalUsers,
                    TotalReviews,
                    BooksThisMonth,
                    NewUsersThisMonth
                }
            };

            // Return data để JavaScript xử lý PDF
            return new JsonResult(reportData);
        }
    }
}