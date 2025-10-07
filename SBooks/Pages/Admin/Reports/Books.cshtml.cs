using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using System.Text.Json;

namespace SBooks.Pages.Admin.Reports
{
    public class BooksModel : AdminPageModel
    {
        public BooksModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty(SupportsGet = true)]
        public string DateRange { get; set; } = "last30days";

        [BindProperty(SupportsGet = true)]
        public string? Genre { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PublisherId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? AuthorId { get; set; }

        // Statistics
        public int TotalBooks { get; set; }
        public decimal AveragePrice { get; set; }
        public int TotalViews { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        // Chart Data
        public string BooksTimelineJson { get; set; } = "[]";
        public string BooksByGenreJson { get; set; } = "[]";
        public string BooksByPriceRangeJson { get; set; } = "[]";
        public string MostViewedBooksJson { get; set; } = "[]";
        public string BooksWithoutReviewsJson { get; set; } = "[]";

        // Filter Options
        public List<string> Genres { get; set; } = new();
        public List<Publisher> Publishers { get; set; } = new();
        public List<Author> Authors { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadFilterOptions();
            await LoadStatistics();
            await LoadChartData();
        }

        private async Task LoadFilterOptions()
        {
            Genres = await _context.Books
                .Where(b => !string.IsNullOrEmpty(b.Genre))
                .Select(b => b.Genre!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            Publishers = await _context.Publishers
                .OrderBy(p => p.PublisherName)
                .ToListAsync();

            Authors = await _context.Authors
                .OrderBy(a => a.AuthorName)
                .ToListAsync();
        }

        private async Task LoadStatistics()
        {
            var query = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(Genre))
                query = query.Where(b => b.Genre == Genre);

            if (PublisherId.HasValue)
                query = query.Where(b => b.PublisherId == PublisherId);

            if (AuthorId.HasValue)
                query = query.Where(b => b.AuthorId == AuthorId);

            TotalBooks = await query.CountAsync();
            AveragePrice = await query.AverageAsync(b => (decimal?)b.Price) ?? 0;
            TotalViews = await query.SumAsync(b => (int?)b.ViewCount) ?? 0;

            // Count total reviews for filtered books
            var bookIds = await query.Select(b => b.BookId).ToListAsync();
            TotalReviews = await _context.Reviews.Where(r => bookIds.Contains(r.BookId)).CountAsync();

            // Set AverageRating to 0 since Rating field doesn't exist in Review model yet
            AverageRating = 0;
        }

        private async Task LoadChartData()
        {
            var (startDate, endDate) = GetDateRange();

            await LoadBooksTimeline(startDate, endDate);
            await LoadBooksByGenre();
            await LoadBooksByPriceRange();
            await LoadMostViewedBooks();
            await LoadBooksWithoutReviews();
        }

        private async Task LoadBooksTimeline(DateTime startDate, DateTime endDate)
        {
            var query = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(Genre))
                query = query.Where(b => b.Genre == Genre);

            var books = await query
                .Where(b => b.CreatedAt.HasValue && b.CreatedAt >= startDate && b.CreatedAt <= endDate)
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
            var query = _context.Books.AsQueryable();

            if (PublisherId.HasValue)
                query = query.Where(b => b.PublisherId == PublisherId);

            var genreStats = await query
                .Where(b => !string.IsNullOrEmpty(b.Genre))
                .GroupBy(b => b.Genre)
                .Select(g => new { Genre = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            BooksByGenreJson = JsonSerializer.Serialize(genreStats);
        }

        private async Task LoadBooksByPriceRange()
        {
            var books = await _context.Books.ToListAsync();

            var priceRanges = new[]
            {
                new { Range = "0-100k", Min = 0m, Max = 100000m },
                new { Range = "100k-300k", Min = 100000m, Max = 300000m },
                new { Range = "300k-500k", Min = 300000m, Max = 500000m },
                new { Range = "500k-1M", Min = 500000m, Max = 1000000m },
                new { Range = "Trên 1M", Min = 1000000m, Max = decimal.MaxValue }
            };

            var priceStats = priceRanges.Select(range => new
            {
                Range = range.Range,
                Count = books.Count(b => b.Price >= range.Min && b.Price < range.Max)
            }).Where(x => x.Count > 0).ToList();

            BooksByPriceRangeJson = JsonSerializer.Serialize(priceStats);
        }

        private async Task LoadMostViewedBooks()
        {
            var query = _context.Books
                .Include(b => b.Author)
                .AsQueryable();

            if (!string.IsNullOrEmpty(Genre))
                query = query.Where(b => b.Genre == Genre);

            var mostViewed = await query
                .OrderByDescending(b => b.ViewCount)
                .Take(10)
                .Select(b => new
                {
                    Title = b.Title,
                    ViewCount = b.ViewCount,
                    Author = b.Author!.AuthorName
                })
                .ToListAsync();

            MostViewedBooksJson = JsonSerializer.Serialize(mostViewed);
        }

        private async Task LoadBooksWithoutReviews()
        {
            var books = await _context.Books
                .Where(b => !_context.Reviews.Any(r => r.BookId == b.BookId) && b.CreatedAt.HasValue)
                .ToListAsync();

            var booksWithoutReviews = books
                .Select(b => new
                {
                    Title = b.Title,
                    CreatedDays = (DateTime.Now - b.CreatedAt!.Value).Days
                })
                .OrderByDescending(x => x.CreatedDays)
                .Take(10)
                .ToList();

            BooksWithoutReviewsJson = JsonSerializer.Serialize(booksWithoutReviews);
        }

        private (DateTime StartDate, DateTime EndDate) GetDateRange()
        {
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
    }
}