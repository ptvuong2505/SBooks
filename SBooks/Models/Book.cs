using System;
using System.Collections.Generic;

namespace SBooks.Models;

public partial class Book
{
    public long BookId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? PublishedYear { get; set; }

    public string? Genre { get; set; }

    public decimal? Price { get; set; }

    public int? ViewCount { get; set; }

    public string? UrlImage { get; set; }

    public long? AuthorId { get; set; }

    public long? PublisherId { get; set; }

    public long? AdminId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? Admin { get; set; }

    public virtual Author? Author { get; set; }

    public virtual ICollection<FavoriteBook> FavoriteBooks { get; set; } = new List<FavoriteBook>();

    public virtual Publisher? Publisher { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
