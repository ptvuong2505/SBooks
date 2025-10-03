using System;
using System.Collections.Generic;

namespace SBooks.Models;

public partial class Author
{
    public long AuthorId { get; set; }

    public string AuthorName { get; set; } = null!;

    public string? Email { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Biography { get; set; }

    public string? Sex { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
