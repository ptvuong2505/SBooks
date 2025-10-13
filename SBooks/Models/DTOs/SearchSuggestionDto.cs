namespace SBooks.Models.DTOs
{
    public class SearchSuggestionDto
    {
        public long BookId { get; set; }
        public string Title { get; set; } = null!;
        public string? AuthorName { get; set; }
        public string? UrlImage { get; set; }
        public double AverageRating { get; set; }
    }
}