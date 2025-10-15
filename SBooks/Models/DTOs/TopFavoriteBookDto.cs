namespace SBooks.Models.DTOs
{
    public class TopFavoriteBookDto
    {
        public long BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? PublishedYear { get; set; }
        public string? Genre { get; set; }
        public decimal? Price { get; set; }
        public string? UrlImage { get; set; }
        public long? AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? PublisherName { get; set; }
        public int FavoriteCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int ViewCount { get; set; }
        public bool IsFavorited { get; set; }
        public int Rank { get; set; } // Thứ hạng trong top
    }
}