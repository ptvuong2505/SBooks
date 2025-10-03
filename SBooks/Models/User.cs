using System;
using System.Collections.Generic;

namespace SBooks.Models;

public partial class User
{
    public long UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? FullName { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    public virtual ICollection<FavoriteBook> FavoriteBooks { get; set; } = new List<FavoriteBook>();

    public virtual ICollection<ReviewVote> ReviewVotes { get; set; } = new List<ReviewVote>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<SearchLog> SearchLogs { get; set; } = new List<SearchLog>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
