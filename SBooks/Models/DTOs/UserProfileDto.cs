namespace SBooks.Models.DTOs
{
    public class UserProfileDto
    {
        public long UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool? IsActive { get; set; }

        // Thống kê
        public int FavoriteBooksCount { get; set; }
        public int ReviewsCount { get; set; }
        public int RepliesCount { get; set; }
        public int SearchHistoryCount { get; set; }
        public DateTime? LastLoginTime { get; set; }

        // Cho Admin
        public int? BooksAddedCount { get; set; }
        public int? TotalViewsOfAddedBooks { get; set; }
        public int? TotalFavoritesOfAddedBooks { get; set; }
    }
}