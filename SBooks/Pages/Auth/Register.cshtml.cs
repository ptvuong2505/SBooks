
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using System.ComponentModel.DataAnnotations;
using BCrypt.Net;

namespace SBooks.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly SbooksContext _context;

        public RegisterModel(SbooksContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RegisterInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public class RegisterInput
        {
            [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3-50 ký tự")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Họ tên là bắt buộc")]
            [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra username đã tồn tại chưa
                    var existingUserByUsername = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username.ToLower() == Input.Username.ToLower());

                    if (existingUserByUsername != null)
                    {
                        ErrorMessage = "Tên đăng nhập đã tồn tại";
                        return Page();
                    }

                    // Kiểm tra email đã tồn tại chưa
                    var existingUserByEmail = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == Input.Email.ToLower());

                    if (existingUserByEmail != null)
                    {
                        ErrorMessage = "Email đã được sử dụng";
                        return Page();
                    }

                    // Tạo user mới
                    var newUser = new User
                    {
                        Username = Input.Username,
                        Email = Input.Email,
                        FullName = Input.FullName,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    // Gán role User mặc định
                    var userRole = await _context.Roles
                        .FirstOrDefaultAsync(r => r.RoleName == "User");

                    if (userRole != null)
                    {
                        newUser.Roles.Add(userRole);
                        await _context.SaveChangesAsync();
                    }

                    SuccessMessage = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
                    ModelState.Clear();
                    Input = new RegisterInput();

                    return Page();
                }
                catch (Exception)
                {
                    ErrorMessage = "Có lỗi xảy ra trong quá trình đăng ký. Vui lòng thử lại.";
                    return Page();
                }
            }

            return Page();
        }
    }
}