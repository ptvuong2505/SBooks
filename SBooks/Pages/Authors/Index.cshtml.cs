using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SBooks.Models;
using SBooks.Services;

namespace SBooks.Pages.Authors
{
    public class IndexModel : PageModel
    {
        private readonly IAuthorService _authorService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IAuthorService authorService, ILogger<IndexModel> logger)
        {
            _authorService = authorService;
            _logger = logger;
        }

        public List<Author> Authors { get; set; } = new();
        public int TotalAuthors { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        public async Task OnGetAsync()
        {
            const int pageSize = 20;

            Authors = await _authorService.GetAllAuthorsAsync(Page, pageSize);
            TotalAuthors = await _authorService.GetTotalAuthorsCountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalAuthors / pageSize);
        }
    }
}