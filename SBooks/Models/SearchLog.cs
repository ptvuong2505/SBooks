using System;
using System.Collections.Generic;

namespace SBooks.Models;

public partial class SearchLog
{
    public long LogId { get; set; }

    public long? UserId { get; set; }

    public string SearchText { get; set; } = null!;

    public DateTime? SearchTime { get; set; }

    public virtual User? User { get; set; }
}
