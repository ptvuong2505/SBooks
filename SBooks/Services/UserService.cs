using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using SBooks.Models.DTOs;

namespace SBooks.Services
{
    public interface IUserService
    {
        Task<UserProfileDto?> GetUserProfileAsync(long userId);
        Task<List<BookCardDto>> GetUserFavoriteBooksAsync(long userId, int page = 1, int pageSize = 10);
        Task<List<Review>> GetUserReviewsAsync(long userId, int page = 1, int pageSize = 10);
        Task<List<SearchLog>> GetUserSearchHistoryAsync(long userId, int page = 1, int pageSize = 20);
        Task<List<BookCardDto>> GetUserAddedBooksAsync(long userId, int page = 1, int pageSize = 10);
        Task<bool> UpdateProfileAsync(long userId, UpdateProfileDto updateDto);
        Task<bool> ChangePasswordAsync(long userId, ChangePasswordDto changePasswordDto);
        Task<bool> RemoveFavoriteBookAsync(long userId, long bookId);
        Task<User?> GetUserByIdAsync(long userId);
    }

    public class UserService : IUserService
    {
        private readonly SbooksContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(SbooksContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(long userId)
        {
            var user = await _context.Users
                .Include(u => u.FavoriteBooks)
                .Include(u => u.Reviews)
                .Include(u => u.SearchLogs)
                .Include(u => u.Books) // Sách được thêm bởi user (nếu là admin)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            var profile = new UserProfileDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                FavoriteBooksCount = user.FavoriteBooks.Count,
                ReviewsCount = user.Reviews.Count(r => r.ParentReviewId == null), // Chỉ đếm review chính, không đếm reply
                RepliesCount = user.Reviews.Count(r => r.ParentReviewId != null), // Đếm reply
                SearchHistoryCount = user.SearchLogs.Count
            };

            // Nếu là admin, tính thêm thống kê về sách đã thêm
            if (user.Books.Any())
            {
                profile.BooksAddedCount = user.Books.Count;
                profile.TotalViewsOfAddedBooks = user.Books.Sum(b => b.ViewCount ?? 0);
                profile.TotalFavoritesOfAddedBooks = await _context.FavoriteBooks
                    .Where(fb => user.Books.Any(b => b.BookId == fb.BookId))
                    .CountAsync();
            }

            // Lấy thời gian đăng nhập cuối từ activity log
            var lastLogin = await _context.ActivityLogs
                .Where(al => al.UserId == userId && al.ActivityType == "LOGIN")
                .OrderByDescending(al => al.ActivityTime)
                .FirstOrDefaultAsync();

            if (lastLogin != null)
            {
                profile.LastLoginTime = lastLogin.ActivityTime;
            }

            return profile;
        }

        public async Task<List<BookCardDto>> GetUserFavoriteBooksAsync(long userId, int page = 1, int pageSize = 10)
        {
            var favoriteBooks = await _context.FavoriteBooks
                .Where(fb => fb.UserId == userId)
                .Include(fb => fb.Book)
                    .ThenInclude(b => b.Author)
                .Include(fb => fb.Book)
                    .ThenInclude(b => b.Reviews)
                .OrderByDescending(fb => fb.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(fb => new BookCardDto
                {
                    BookId = fb.Book.BookId,
                    Title = fb.Book.Title,
                    AuthorName = fb.Book.Author != null ? fb.Book.Author.AuthorName : "Không rõ",
                    AuthorId = fb.Book.AuthorId,
                    Genre = fb.Book.Genre,
                    Price = fb.Book.Price,
                    UrlImage = fb.Book.UrlImage,
                    ViewCount = fb.Book.ViewCount ?? 0,
                    AverageRating = fb.Book.Reviews.Where(r => r.Rating.HasValue && r.ParentReviewId == null)
                        .Average(r => (double?)r.Rating) ?? 0,
                    ReviewCount = fb.Book.Reviews.Count(r => r.ParentReviewId == null),
                    IsFavorite = true,
                    FavoriteDate = fb.CreatedAt
                })
                .ToListAsync();

            return favoriteBooks;
        }

        public async Task<List<Review>> GetUserReviewsAsync(long userId, int page = 1, int pageSize = 10)
        {
            return await _context.Reviews
                .Where(r => r.UserId == userId)
                .Include(r => r.Book)
                .Include(r => r.ParentReview)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<SearchLog>> GetUserSearchHistoryAsync(long userId, int page = 1, int pageSize = 20)
        {
            return await _context.SearchLogs
                .Where(sl => sl.UserId == userId)
                .OrderByDescending(sl => sl.SearchTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<BookCardDto>> GetUserAddedBooksAsync(long userId, int page = 1, int pageSize = 10)
        {
            return await _context.Books
                .Where(b => b.AdminId == userId)
                .Include(b => b.Author)
                .Include(b => b.Reviews)
                .Include(b => b.FavoriteBooks)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookCardDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    AuthorName = b.Author != null ? b.Author.AuthorName : "Không rõ",
                    AuthorId = b.AuthorId,
                    Genre = b.Genre,
                    Price = b.Price,
                    UrlImage = b.UrlImage,
                    ViewCount = b.ViewCount ?? 0,
                    AverageRating = b.Reviews.Where(r => r.Rating.HasValue && r.ParentReviewId == null)
                        .Average(r => (double?)r.Rating) ?? 0,
                    ReviewCount = b.Reviews.Count(r => r.ParentReviewId == null),
                    FavoriteCount = b.FavoriteBooks.Count,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateProfileAsync(long userId, UpdateProfileDto updateDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                // Kiểm tra email đã tồn tại chưa (trừ email của chính user này)
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == updateDto.Email && u.UserId != userId);
                if (emailExists) return false;

                user.FullName = updateDto.FullName;
                user.Email = updateDto.Email;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(long userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                // Verify current password (trong thực tế cần hash và verify)
                // Tạm thời so sánh trực tiếp cho test
                if (user.PasswordHash != changePasswordDto.CurrentPassword)
                    return false;

                // Update password (trong thực tế cần hash)
                user.PasswordHash = changePasswordDto.NewPassword;

                await _context.SaveChangesAsync();

                // Log activity
                var activityLog = new ActivityLog
                {
                    UserId = userId,
                    ActivityType = "CHANGE_PASSWORD",
                    Details = "User changed password",
                    ActivityTime = DateTime.UtcNow
                };
                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> RemoveFavoriteBookAsync(long userId, long bookId)
        {
            try
            {
                var favoriteBook = await _context.FavoriteBooks
                    .FirstOrDefaultAsync(fb => fb.UserId == userId && fb.BookId == bookId);

                if (favoriteBook == null) return false;

                _context.FavoriteBooks.Remove(favoriteBook);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite book for user {UserId}, book {BookId}", userId, bookId);
                return false;
            }
        }

        public async Task<User?> GetUserByIdAsync(long userId)
        {
            return await _context.Users.FindAsync(userId);
        }
    }
}