using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SBooks.Extensions;
using SBooks.Models.DTOs;
using SBooks.Services;

namespace SBooks.Pages.Favorites
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IUserService userService, ILogger<IndexModel> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public List<BookCardDto> FavoriteBooks { get; set; } = new();
        public int TotalBooks { get; set; }
        public int PageSize { get; set; } = 12;

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return RedirectToPage("/Auth/Login");

            FavoriteBooks = await _userService.GetUserFavoriteBooksAsync(userId.Value, PageNumber, PageSize);

            // Để tính tổng số sách yêu thích, có thể thêm method GetUserFavoriteBooksCountAsync
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveFavoriteAsync(long bookId)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return new JsonResult(new { success = false, message = "Bạn cần đăng nhập" });

            var success = await _userService.RemoveFavoriteBookAsync(userId.Value, bookId);
            if (success)
            {
                return new JsonResult(new { success = true, message = "Đã xóa khỏi danh sách yêu thích" });
            }

            return new JsonResult(new { success = false, message = "Có lỗi xảy ra" });
        }
    }
}