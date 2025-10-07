using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Publishers
{
    public class IndexModel : AdminPageModel
    {
        public IndexModel(SbooksContext context) : base(context)
        {
        }

        public IList<Publisher> Publishers { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "PublisherName";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "asc";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 12;
        public int TotalPages { get; set; }
        public int TotalPublishers { get; set; }
        public int FilteredPublishers { get; set; }

        public async Task OnGetAsync()
        {
            await LoadPublishers();
        }

        private async Task LoadPublishers()
        {
            var query = _context.Publishers
                .Include(p => p.Books)
                .AsQueryable();

            // Đếm tổng số publishers
            TotalPublishers = await _context.Publishers.CountAsync();

            // Áp dụng bộ lọc tìm kiếm
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(p =>
                    p.PublisherName.ToLower().Contains(searchLower) ||
                    (p.Address != null && p.Address.ToLower().Contains(searchLower)) ||
                    (p.Website != null && p.Website.ToLower().Contains(searchLower)));
            }

            // Đếm số publishers sau khi lọc
            FilteredPublishers = await query.CountAsync();

            // Áp dụng sắp xếp
            query = SortDirection.ToLower() == "desc" ? ApplySortingDesc(query) : ApplySortingAsc(query);

            // Tính toán phân trang
            TotalPages = (int)Math.Ceiling((double)FilteredPublishers / PageSize);

            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Áp dụng phân trang
            Publishers = await query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        private IQueryable<Publisher> ApplySortingAsc(IQueryable<Publisher> query)
        {
            return SortBy.ToLower() switch
            {
                "publishername" => query.OrderBy(p => p.PublisherName),
                "address" => query.OrderBy(p => p.Address ?? ""),
                "website" => query.OrderBy(p => p.Website ?? ""),
                "bookcount" => query.OrderBy(p => p.Books.Count),
                _ => query.OrderBy(p => p.PublisherName)
            };
        }

        private IQueryable<Publisher> ApplySortingDesc(IQueryable<Publisher> query)
        {
            return SortBy.ToLower() switch
            {
                "publishername" => query.OrderByDescending(p => p.PublisherName),
                "address" => query.OrderByDescending(p => p.Address ?? ""),
                "website" => query.OrderByDescending(p => p.Website ?? ""),
                "bookcount" => query.OrderByDescending(p => p.Books.Count),
                _ => query.OrderByDescending(p => p.PublisherName)
            };
        }
    }
}