
using Microsoft.AspNetCore.Mvc.RazorPages;
using SBooks.Services;

namespace SBooks.Pages.Books
{
    public class TopFavoriteBooksModel : PageModel
    {
        private readonly IBookService _bookService;

        public TopFavoriteBooksModel(IBookService bookService)
        {
            _bookService = bookService;
        }
    }
}

