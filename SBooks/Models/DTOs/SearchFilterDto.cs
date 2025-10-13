namespace SBooks.Models.DTOs
{
    public class SearchFilterDto
    {
        public string? SearchQuery { get; set; }
        public List<long> AuthorIds { get; set; } = new();
        public List<string> Genres { get; set; } = new();
        public string SortBy { get; set; } = "newest"; // newest, oldest, rating, title, favorites
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}