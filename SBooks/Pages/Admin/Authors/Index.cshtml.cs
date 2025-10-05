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

        public async Task OnGetAsync()
        {
            Authors = await _context.Authors
                .Include(a => a.Books)
                .OrderBy(a => a.AuthorName)
                .ToListAsync();
        }
    }
}