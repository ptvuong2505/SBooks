
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;

namespace SBooks.Auth
{
    public class LoginModel(SbooksContext _context) : PageModel
    {

        [BindProperty]
        public string ErrorMessage { get; set; } = string.Empty;

        [BindProperty, Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [BindProperty, Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; } = false;

        public void OnGet()
        {
            ErrorMessage = "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = _context.Users.Include(u => u.Roles)
                .FirstOrDefault(u => u.Username == Username && u.IsActive == true);


            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                ErrorMessage = "Invalid username or password.";
                return Page();
            }
            else if (user.IsActive == false)
            {
                ErrorMessage = "Your account is locked.";
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Roles.FirstOrDefault()?.RoleName ?? "User")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = RememberMe,
                ExpiresUtc = RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddMinutes(60)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            if (user.Roles.Any(r => r.RoleName == "Admin"))
            {
                return RedirectToPage("/Admin/Dashboard/Index");
            }
            else
            {
                return RedirectToPage("/Index");
            }
        }
    }
}