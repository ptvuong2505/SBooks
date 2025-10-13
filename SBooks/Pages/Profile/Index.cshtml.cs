using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SBooks.Extensions;
using SBooks.Models.DTOs;
using SBooks.Services;

namespace SBooks.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IBookService _bookService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IUserService userService, IBookService bookService, ILogger<IndexModel> logger)
        {
            _userService = userService;
            _bookService = bookService;
            _logger = logger;
        }

        public UserProfileDto Profile { get; set; } = new();
        public List<BookCardDto> FavoriteBooks { get; set; } = new();
        public List<Models.Review> RecentReviews { get; set; } = new();
        public List<Models.SearchLog> RecentSearches { get; set; } = new();
        public List<BookCardDto> AddedBooks { get; set; } = new(); // Cho Admin
        public List<BookCardDto> TopFavoriteBooks { get; set; } = new(); // Top sách yêu thích

        [BindProperty]
        public UpdateProfileDto UpdateProfile { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return RedirectToPage("/Auth/Login");

            await LoadProfileDataAsync(userId.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return RedirectToPage("/Auth/Login");

            if (!ModelState.IsValid)
            {
                await LoadProfileDataAsync(userId.Value);
                return Page();
            }

            var success = await _userService.UpdateProfileAsync(userId.Value, UpdateProfile);
            if (success)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra hoặc email đã tồn tại!";
            }

            return RedirectToPage();
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

        public async Task<IActionResult> OnGetLoadFavoriteBooksAsync(int page = 1)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return new JsonResult(new { success = false });

            var books = await _userService.GetUserFavoriteBooksAsync(userId.Value, page, 6);
            return Partial("_FavoriteBooks", books);
        }

        public async Task<IActionResult> OnGetLoadAddedBooksAsync(int page = 1)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return new JsonResult(new { success = false });

            var books = await _userService.GetUserAddedBooksAsync(userId.Value, page, 6);
            return Partial("_AddedBooks", books);
        }

        private async Task LoadProfileDataAsync(long userId)
        {
            Profile = await _userService.GetUserProfileAsync(userId) ?? new UserProfileDto();

            // Load dữ liệu cho form update
            UpdateProfile = new UpdateProfileDto
            {
                FullName = Profile.FullName ?? "",
                Email = Profile.Email
            };

            // Load sách yêu thích gần đây
            FavoriteBooks = await _userService.GetUserFavoriteBooksAsync(userId, 1, 6);

            // Load đánh giá gần đây
            RecentReviews = await _userService.GetUserReviewsAsync(userId, 1, 5);

            // Load lịch sử tìm kiếm gần đây
            RecentSearches = await _userService.GetUserSearchHistoryAsync(userId, 1, 10);

            // Nếu là admin, load sách đã thêm
            if (User.IsAdmin())
            {
                AddedBooks = await _userService.GetUserAddedBooksAsync(userId, 1, 6);
            }

            // Load top sách yêu thích
            TopFavoriteBooks = await _bookService.GetTopFavoriteBooksAsync(10, userId);
        }
    }
}