using System;
using System.Collections.Generic;

namespace SBooks.Models;

public partial class Publisher
{
    public long PublisherId { get; set; }

    public string PublisherName { get; set; } = null!;

    public string? Address { get; set; }

    public string? Website { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
