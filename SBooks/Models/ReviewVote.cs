﻿using System;
using System.Collections.Generic;

namespace SBooks.Models;

public partial class ReviewVote
{
    public long UserId { get; set; }

    public long ReviewId { get; set; }

    public short VoteType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Review Review { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
