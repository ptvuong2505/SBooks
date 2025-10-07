using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Users
{
    public class DetailsModel : AdminPageModel
    {
        public DetailsModel(SbooksContext context) : base(context)
        {
        }

        public User User { get; set; } = default!;
        public IList<Book> UserBooks { get; set; } = default!;
        public IList<Review> UserReviews { get; set; } = default!;
        public IList<ActivityLog> RecentActivities { get; set; } = default!;
        public int TotalFavorites { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Roles)
                .Include(u => u.Books)
                    .ThenInclude(b => b.Publisher)
                .Include(u => u.Reviews)
                    .ThenInclude(r => r.Book)
                .Include(u => u.FavoriteBooks)
                    .ThenInclude(f => f.Book)
                .Include(u => u.ActivityLogs.OrderByDescending(a => a.ActivityTime).Take(10))
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            User = user;
            UserBooks = user.Books.OrderByDescending(b => b.CreatedAt).Take(5).ToList();
            UserReviews = user.Reviews.OrderByDescending(r => r.UpdatedAt).Take(5).ToList();
            RecentActivities = user.ActivityLogs.OrderByDescending(a => a.ActivityTime).Take(10).ToList();
            TotalFavorites = user.FavoriteBooks.Count;

            return Page();
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

        public int GetUserAge()
        {
            if (User.CreatedAt.HasValue)
            {
                return (DateTime.Now - User.CreatedAt.Value).Days;
            }
            return 0;
        }
    }
}