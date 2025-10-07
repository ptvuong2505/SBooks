using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Users
{
    public class DeleteModel : AdminPageModel
    {
        public DeleteModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty]
        public User User { get; set; } = default!;

        public int BookCount { get; set; }
        public int ReviewCount { get; set; }
        public int FavoriteCount { get; set; }
        public int ActivityLogCount { get; set; }
        public bool HasDependencies { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Roles)
                .Include(u => u.Books)
                .Include(u => u.Reviews)
                .Include(u => u.FavoriteBooks)
                .Include(u => u.ActivityLogs)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            User = user;
            BookCount = user.Books.Count;
            ReviewCount = user.Reviews.Count;
            FavoriteCount = user.FavoriteBooks.Count;
            ActivityLogCount = user.ActivityLogs.Count;

            // Kiểm tra có dữ liệu liên quan không
            HasDependencies = BookCount > 0 || ReviewCount > 0 || FavoriteCount > 0 || ActivityLogCount > 0;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Roles)
                .Include(u => u.Books)
                .Include(u => u.Reviews)
                .Include(u => u.FavoriteBooks)
                .Include(u => u.ActivityLogs)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Kiểm tra lại các ràng buộc trước khi xóa
                if (user.Books.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa người dùng này vì có sách liên quan. Hãy xóa hoặc chuyển đổi các sách trước.";
                    return RedirectToPage("./Index");
                }

                // Xóa các dữ liệu liên quan (reviews, favorites, activity logs)
                _context.Reviews.RemoveRange(user.Reviews);
                _context.FavoriteBooks.RemoveRange(user.FavoriteBooks);
                _context.ActivityLogs.RemoveRange(user.ActivityLogs);

                // Xóa các vai trò
                user.Roles.Clear();

                // Xóa user
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Xóa người dùng '{user.Username}' thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa người dùng: " + ex.Message;
                return RedirectToPage("./Index");
            }
        }

        public string GetUserRole()
        {
            if (User.Roles.Any(r => r.RoleName == "Admin"))
                return "Quản trị viên";
            if (User.Roles.Any(r => r.RoleName == "User"))
                return "Người dùng";
            return "Chưa phân quyền";
        }

        public string GetUserRoleBadgeClass()
        {
            if (User.Roles.Any(r => r.RoleName == "Admin"))
                return "bg-danger";
            if (User.Roles.Any(r => r.RoleName == "User"))
                return "bg-success";
            return "bg-secondary";
        }
    }
}