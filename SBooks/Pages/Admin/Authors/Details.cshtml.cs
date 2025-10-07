using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Authors
{
    public class DetailsModel : AdminPageModel
    {
        public DetailsModel(SbooksContext context) : base(context)
        {
        }

        public Author Author { get; set; } = default!;
        public List<Book> AuthorBooks { get; set; } = new();
        public int TotalBooksCount { get; set; }
        public int TotalViewsCount { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                .Include(a => a.Books)
                    .ThenInclude(b => b.Publisher)
                .FirstOrDefaultAsync(m => m.AuthorId == id);

            if (author == null)
            {
                return NotFound();
            }

            Author = author;
            AuthorBooks = author.Books.OrderByDescending(b => b.CreatedAt).ToList();
            TotalBooksCount = author.Books.Count;
            TotalViewsCount = author.Books.Sum(b => b.ViewCount ?? 0);

            return Page();
        }
    }
}