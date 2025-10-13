using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SBooks.Extensions;
using SBooks.Models.DTOs;
using SBooks.Services;

namespace SBooks.Pages.Auth
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(IUserService userService, ILogger<ChangePasswordModel> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [BindProperty]
        public ChangePasswordDto ChangePassword { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public IActionResult OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Page("/Profile/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Page("/Profile/Index");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return RedirectToPage("/Auth/Login");
            }

            var success = await _userService.ChangePasswordAsync(userId.Value, ChangePassword);

            if (success)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return LocalRedirect(ReturnUrl ?? "/Profile/Index");
            }
            else
            {
                ModelState.AddModelError("ChangePassword.CurrentPassword", "Mật khẩu hiện tại không đúng");
                return Page();
            }
        }
    }
}