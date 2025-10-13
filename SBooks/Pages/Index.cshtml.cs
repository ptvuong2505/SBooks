using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SBooks.Models.DTOs;
using SBooks.Services;
using SBooks.Extensions;
using SBooks.Models;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;

namespace SBooks.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IBookService _bookService;
    private readonly SbooksContext _context;

    public IndexModel(ILogger<IndexModel> logger, IBookService bookService, SbooksContext context)
    {
        _logger = logger;
        _bookService = bookService;
        _context = context;
    }

    public PaginatedResult<BookCardDto> Books { get; set; } = new();
    public List<Author> Authors { get; set; } = new();
    public List<string> Genres { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<long> AuthorIds { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedGenres { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "newest";

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public async Task OnGetAsync()
    {
        await LoadFilterDataAsync();
        await LoadBooksAsync();

        // Log search nếu có search query
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var userId = User.GetUserId();
            await _bookService.LogSearchAsync(SearchQuery, userId);
        }
    }

    public async Task<IActionResult> OnGetSearchSuggestionsAsync(string query)
    {
        var suggestions = await _bookService.GetSearchSuggestionsAsync(query);
        return new JsonResult(suggestions);
    }

    public async Task<IActionResult> OnGetLoadBooksAsync()
    {
        await LoadBooksAsync();
        return Partial("_BookGrid", Books);
    }

    private async Task LoadFilterDataAsync()
    {
        Authors = await _bookService.GetAuthorsAsync();
        Genres = await _bookService.GetGenresAsync();
    }

    private async Task LoadBooksAsync()
    {
        var filter = new SearchFilterDto
        {
            SearchQuery = SearchQuery,
            AuthorIds = AuthorIds,
            Genres = SelectedGenres,
            SortBy = SortBy,
            Page = CurrentPage,
            PageSize = 12
        };

        var userId = User.GetUserId();
        Books = await _bookService.GetBooksAsync(filter, userId);
    }
}
