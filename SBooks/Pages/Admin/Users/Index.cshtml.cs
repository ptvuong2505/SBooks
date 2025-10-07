using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Users
{
    public class IndexModel : AdminPageModel
    {
        public IndexModel(SbooksContext context) : base(context)
        {
        }

        public IList<User> Users { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "Username";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "asc";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 15;
        public int TotalPages { get; set; }
        public int TotalUsers { get; set; }
        public int FilteredUsers { get; set; }

        public async Task OnGetAsync()
        {
            await LoadUsers();
        }

        private async Task LoadUsers()
        {
            var query = _context.Users
                .Include(u => u.Roles)
                .Include(u => u.Books)
                .Include(u => u.Reviews)
                .AsQueryable();

            // Đếm tổng số users
            TotalUsers = await _context.Users.CountAsync();

            // Áp dụng bộ lọc tìm kiếm
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(searchLower)));
            }

            // Áp dụng bộ lọc trạng thái
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                switch (StatusFilter.ToLower())
                {
                    case "active":
                        query = query.Where(u => u.IsActive == true);
                        break;
                    case "inactive":
                        query = query.Where(u => u.IsActive == false);
                        break;
                    case "admin":
                        query = query.Where(u => u.Roles.Any(r => r.RoleName == "Admin"));
                        break;
                    case "user":
                        query = query.Where(u => u.Roles.Any(r => r.RoleName == "User") && !u.Roles.Any(r => r.RoleName == "Admin"));
                        break;
                }
            }

            // Đếm số users sau khi lọc
            FilteredUsers = await query.CountAsync();

            // Áp dụng sắp xếp
            query = SortDirection.ToLower() == "desc" ? ApplySortingDesc(query) : ApplySortingAsc(query);

            // Tính toán phân trang
            TotalPages = (int)Math.Ceiling((double)FilteredUsers / PageSize);

            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Áp dụng phân trang
            Users = await query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        private IQueryable<User> ApplySortingAsc(IQueryable<User> query)
        {
            return SortBy.ToLower() switch
            {
                "username" => query.OrderBy(u => u.Username),
                "email" => query.OrderBy(u => u.Email),
                "fullname" => query.OrderBy(u => u.FullName ?? u.Username),
                "createdat" => query.OrderBy(u => u.CreatedAt),
                "bookcount" => query.OrderBy(u => u.Books.Count),
                "reviewcount" => query.OrderBy(u => u.Reviews.Count),
                _ => query.OrderBy(u => u.Username)
            };
        }

        private IQueryable<User> ApplySortingDesc(IQueryable<User> query)
        {
            return SortBy.ToLower() switch
            {
                "username" => query.OrderByDescending(u => u.Username),
                "email" => query.OrderByDescending(u => u.Email),
                "fullname" => query.OrderByDescending(u => u.FullName ?? u.Username),
                "createdat" => query.OrderByDescending(u => u.CreatedAt),
                "bookcount" => query.OrderByDescending(u => u.Books.Count),
                "reviewcount" => query.OrderByDescending(u => u.Reviews.Count),
                _ => query.OrderByDescending(u => u.Username)
            };
        }
    }
}