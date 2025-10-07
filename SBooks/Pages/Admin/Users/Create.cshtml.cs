using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using System.ComponentModel.DataAnnotations;

namespace SBooks.Pages.Admin.Users
{
    public class CreateModel : AdminPageModel
    {
        private readonly IPasswordHasher<User> _passwordHasher;

        public CreateModel(SbooksContext context, IPasswordHasher<User> passwordHasher) : base(context)
        {
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public UserInputModel Input { get; set; } = default!;

        public IList<Role> AvailableRoles { get; set; } = default!;

        public class UserInputModel
        {
            [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
            [StringLength(50, ErrorMessage = "Tên đăng nhập không được quá 50 ký tự")]
            [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ, số và dấu gạch dưới")]
            public string Username { get; set; } = null!;

            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
            [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
            public string Email { get; set; } = null!;

            [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
            public string? FullName { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = null!;

            [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Xác nhận mật khẩu không khớp")]
            public string ConfirmPassword { get; set; } = null!;

            public bool IsActive { get; set; } = true;

            public List<long> SelectedRoleIds { get; set; } = new List<long>();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadData();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Kiểm tra username trùng lặp
            if (await _context.Users.AnyAsync(u => u.Username == Input.Username))
            {
                ModelState.AddModelError("Input.Username", "Tên đăng nhập đã tồn tại");
                return Page();
            }

            // Kiểm tra email trùng lặp
            if (await _context.Users.AnyAsync(u => u.Email == Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Email đã được sử dụng");
                return Page();
            }

            var user = new User
            {
                Username = Input.Username,
                Email = Input.Email,
                FullName = Input.FullName,
                IsActive = Input.IsActive,
                CreatedAt = DateTime.Now
            };

            // Hash password
            user.PasswordHash = _passwordHasher.HashPassword(user, Input.Password);

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Thêm vai trò cho user
                if (Input.SelectedRoleIds.Any())
                {
                    var roles = await _context.Roles
                        .Where(r => Input.SelectedRoleIds.Contains(r.RoleId))
                        .ToListAsync();

                    user.Roles = roles;
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thêm người dùng thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm người dùng: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnGetCheckUsernameAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new { available = true });
            }

            var exists = await _context.Users.AnyAsync(u => u.Username == username);
            return new JsonResult(new { available = !exists });
        }

        public async Task<IActionResult> OnGetCheckEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return new JsonResult(new { available = true });
            }

            var exists = await _context.Users.AnyAsync(u => u.Email == email);
            return new JsonResult(new { available = !exists });
        }

        private async Task LoadData()
        {
            AvailableRoles = await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();
        }
    }
}