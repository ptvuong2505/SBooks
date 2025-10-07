using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Publishers
{
    public class DetailsModel : AdminPageModel
    {
        public DetailsModel(SbooksContext context) : base(context)
        {
        }

        public Publisher Publisher { get; set; } = default!;
        public IList<Book> RecentBooks { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publisher = await _context.Publishers
                .Include(p => p.Books)
                    .ThenInclude(b => b.Author)
                .FirstOrDefaultAsync(m => m.PublisherId == id);

            if (publisher == null)
            {
                return NotFound();
            }

            Publisher = publisher;
            RecentBooks = publisher.Books
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToList();

            return Page();
        }

        public int GetTotalBooks()
        {
            return Publisher.Books.Count;
        }

        public Book? GetNewestBook()
        {
            return Publisher.Books
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefault();
        }

        public Book? GetOldestBook()
        {
            return Publisher.Books
                .OrderBy(b => b.CreatedAt)
                .FirstOrDefault();
        }

        public IEnumerable<IGrouping<int?, Book>> GetBooksByYear()
        {
            return Publisher.Books
                .GroupBy(b => b.PublishedYear)
                .OrderByDescending(g => g.Key);
        }
    }
}