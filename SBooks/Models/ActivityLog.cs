using System;
using System.Collections.Generic;

namespace SBooks.Models;

public partial class ActivityLog
{
    public long LogId { get; set; }

    public long UserId { get; set; }

    public string ActivityType { get; set; } = null!;

    public string? Details { get; set; }

    public DateTime? ActivityTime { get; set; }

    public virtual User User { get; set; } = null!;
}
