using System;

namespace SBooks.Models.DTOs
{
    public class BookCardDto
    {
        public long BookId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int? PublishedYear { get; set; }
        public string? Genre { get; set; }
        public decimal? Price { get; set; }
        public string? UrlImage { get; set; }
        public string? AuthorName { get; set; }
        public long? AuthorId { get; set; } // Để tạo link đến trang tác giả
        public string? PublisherName { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int FavoriteCount { get; set; }
        public bool IsFavorited { get; set; } // Để check user hiện tại đã favorite chưa
        public bool IsFavorite { get; set; } // For compatibility
        public DateTime? FavoriteDate { get; set; } // Ngày thêm vào yêu thích
        public DateTime? CreatedAt { get; set; } // Ngày tạo sách
        public int ViewCount { get; set; } // Lượt xem
    }
}