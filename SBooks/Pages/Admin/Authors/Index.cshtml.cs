using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Authors
{
    public class IndexModel : AdminPageModel
    {
        public IndexModel(SbooksContext context) : base(context)
        {
        }

        public IList<Author> Authors { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchEmail { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; } = "AuthorName";

        [BindProperty(SupportsGet = true)]
        public string? SortDirection { get; set; } = "asc";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int CurrentPage => PageNumber;
        public int TotalPages => (int)Math.Ceiling((double)FilteredAuthors / PageSize);
        public int PageSize => 12;

        public int TotalAuthors { get; set; }
        public int FilteredAuthors { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAuthors();
        }

        private async Task LoadAuthors()
        {
            var query = _context.Authors
                .Include(a => a.Books)
                .AsQueryable();

            // Apply search filters
            if (!string.IsNullOrEmpty(SearchName))
            {
                query = query.Where(a => a.AuthorName.Contains(SearchName));
            }

            if (!string.IsNullOrEmpty(SearchEmail))
            {
                query = query.Where(a => a.Email != null && a.Email.Contains(SearchEmail));
            }

            // Get counts
            TotalAuthors = await _context.Authors.CountAsync();
            FilteredAuthors = await query.CountAsync();

            // Apply sorting
            query = SortBy?.ToLower() switch
            {
                "authorname" => SortDirection == "desc" ? query.OrderByDescending(a => a.AuthorName) : query.OrderBy(a => a.AuthorName),
                "email" => SortDirection == "desc" ? query.OrderByDescending(a => a.Email) : query.OrderBy(a => a.Email),
                "bookcount" => SortDirection == "desc" ? query.OrderByDescending(a => a.Books.Count) : query.OrderBy(a => a.Books.Count),
                "birthdate" => SortDirection == "desc" ? query.OrderByDescending(a => a.BirthDate) : query.OrderBy(a => a.BirthDate),
                _ => query.OrderBy(a => a.AuthorName)
            };

            // Apply pagination
            var skip = (PageNumber - 1) * PageSize;
            Authors = await query.Skip(skip).Take(PageSize).ToListAsync();
        }
    }
}