using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SBooks.Extensions;
using SBooks.Models;
using SBooks.Models.DTOs;
using SBooks.Services;

namespace SBooks.Pages.Authors
{
    public class DetailsModel : PageModel
    {
        private readonly IAuthorService _authorService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IAuthorService authorService, ILogger<DetailsModel> logger)
        {
            _authorService = authorService;
            _logger = logger;
        }

        public Author Author { get; set; } = new();
        public AuthorStatsDto Stats { get; set; } = new();
        public PaginatedResult<BookCardDto> Books { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Author = await _authorService.GetAuthorByIdAsync(id);
            if (Author == null)
            {
                return NotFound();
            }

            Stats = await _authorService.GetAuthorStatsAsync(id);

            var userId = User.GetUserId();
            Books = await _authorService.GetBooksByAuthorAsync(id, Page, 12, userId);

            return Page();
        }

        public async Task<IActionResult> OnGetLoadMoreBooksAsync(long id, int page)
        {
            var userId = User.GetUserId();
            var books = await _authorService.GetBooksByAuthorAsync(id, page, 12, userId);
            return Partial("_AuthorBooks", books.Items);
        }
    }
}