
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SBooks.Auth
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string ErrorMessage { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; } = false;

        public void OnGet()
        {
            ErrorMessage = "";
        }

        public void OnPost()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please fill in all required fields.";
                return;
            }

            if (Username == "admin" && Password == "password")
            {
                // In a real application, set up authentication cookies or tokens here
                ErrorMessage = "Login successful!";
            }
            else
            {
                ErrorMessage = "Invalid username or password.";
            }
        }
    }
};