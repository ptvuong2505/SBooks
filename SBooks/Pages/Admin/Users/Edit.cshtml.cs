using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using System.ComponentModel.DataAnnotations;

namespace SBooks.Pages.Admin.Users
{
    public class EditModel : AdminPageModel
    {
        private readonly IPasswordHasher<User> _passwordHasher;

        public EditModel(SbooksContext context, IPasswordHasher<User> passwordHasher) : base(context)
        {
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public UserInputModel Input { get; set; } = default!;

        public User User { get; set; } = default!;
        public IList<Role> AvailableRoles { get; set; } = default!;

        public class UserInputModel
        {
            public long UserId { get; set; }

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

            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
            [DataType(DataType.Password)]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp")]
            public string? ConfirmNewPassword { get; set; }

            public bool IsActive { get; set; }

            public List<long> SelectedRoleIds { get; set; } = new List<long>();
        }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            User = user;
            Input = new UserInputModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive ?? true,
                SelectedRoleIds = user.Roles.Select(r => (long)r.RoleId).ToList()
            };

            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadData();

            if (!ModelState.IsValid)
            {
                User = await _context.Users
                    .Include(u => u.Roles)
                    .FirstOrDefaultAsync(m => m.UserId == Input.UserId);
                return Page();
            }

            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(m => m.UserId == Input.UserId);

            if (user == null)
            {
                return NotFound();
            }

            // Kiểm tra username trùng lặp (trừ chính user này)
            if (await _context.Users.AnyAsync(u => u.Username == Input.Username && u.UserId != Input.UserId))
            {
                ModelState.AddModelError("Input.Username", "Tên đăng nhập đã tồn tại");
                User = user;
                return Page();
            }

            // Kiểm tra email trùng lặp (trừ chính user này)
            if (await _context.Users.AnyAsync(u => u.Email == Input.Email && u.UserId != Input.UserId))
            {
                ModelState.AddModelError("Input.Email", "Email đã được sử dụng");
                User = user;
                return Page();
            }

            try
            {
                // Cập nhật thông tin cơ bản
                user.Username = Input.Username;
                user.Email = Input.Email;
                user.FullName = Input.FullName;
                user.IsActive = Input.IsActive;

                // Cập nhật mật khẩu nếu có
                if (!string.IsNullOrEmpty(Input.NewPassword))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, Input.NewPassword);
                }

                // Cập nhật vai trò
                user.Roles.Clear();
                if (Input.SelectedRoleIds.Any())
                {
                    var roles = await _context.Roles
                        .Where(r => Input.SelectedRoleIds.Contains(r.RoleId))
                        .ToListAsync();

                    foreach (var role in roles)
                    {
                        user.Roles.Add(role);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật người dùng: " + ex.Message);
                User = user;
                return Page();
            }
        }

        public async Task<IActionResult> OnGetCheckUsernameAsync(string username, long userId)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new { available = true });
            }

            var exists = await _context.Users.AnyAsync(u => u.Username == username && u.UserId != userId);
            return new JsonResult(new { available = !exists });
        }

        public async Task<IActionResult> OnGetCheckEmailAsync(string email, long userId)
        {
            if (string.IsNullOrEmpty(email))
            {
                return new JsonResult(new { available = true });
            }

            var exists = await _context.Users.AnyAsync(u => u.Email == email && u.UserId != userId);
            return new JsonResult(new { available = !exists });
        }

        private async Task LoadData()
        {
            AvailableRoles = await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();
        }
    }
}