using System;
using System.Collections.Generic;

namespace SBooks.Models;

public partial class Review
{
    public long ReviewId { get; set; }

    public long BookId { get; set; }

    public long UserId { get; set; }

    public long? ParentReviewId { get; set; }

    public string? CommentText { get; set; }

    public int? LikeCount { get; set; }

    public int? DislikeCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public short? Rating { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual ICollection<Review> InverseParentReview { get; set; } = new List<Review>();

    public virtual Review? ParentReview { get; set; }

    public virtual ICollection<ReviewVote> ReviewVotes { get; set; } = new List<ReviewVote>();

    public virtual User User { get; set; } = null!;
}
