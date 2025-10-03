using System;
using System.Collections.Generic;

namespace SBooks.Models;

public partial class FavoriteBook
{
    public long UserId { get; set; }

    public long BookId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
