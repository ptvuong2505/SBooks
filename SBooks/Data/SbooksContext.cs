using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SBooks.Models;

namespace SBooks.Data;

public partial class SbooksContext : DbContext
{
    public SbooksContext()
    {
    }

    public SbooksContext(DbContextOptions<SbooksContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<FavoriteBook> FavoriteBooks { get; set; }

    public virtual DbSet<Publisher> Publishers { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<ReviewVote> ReviewVotes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SearchLog> SearchLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    //     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //         => optionsBuilder.UseNpgsql("Host=localhost;Database=sbooks;Username=postgres;Password=123456");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("activity_logs_pkey");

            entity.ToTable("activity_logs");

            entity.HasIndex(e => e.UserId, "idx_activity_logs_user_id");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.ActivityTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("activity_time");
            entity.Property(e => e.ActivityType).HasColumnName("activity_type");
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("activity_logs_user_id_fkey");
        });

        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.AuthorId).HasName("authors_pkey");

            entity.ToTable("authors");

            entity.HasIndex(e => e.Email, "authors_email_key").IsUnique();

            entity.HasIndex(e => e.AuthorName, "idx_authors_author_name");

            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.AuthorName)
                .HasMaxLength(255)
                .HasColumnName("author_name");
            entity.Property(e => e.Biography).HasColumnName("biography");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Sex)
                .HasMaxLength(20)
                .HasColumnName("sex");
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("books_pkey");

            entity.ToTable("books");

            entity.HasIndex(e => e.AdminId, "idx_books_admin_id");

            entity.HasIndex(e => e.AuthorId, "idx_books_author_id");

            entity.HasIndex(e => e.Genre, "idx_books_genre");

            entity.HasIndex(e => e.PublisherId, "idx_books_publisher_id");

            entity.HasIndex(e => e.Title, "idx_books_title");

            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Genre)
                .HasMaxLength(255)
                .HasColumnName("genre");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0.00")
                .HasColumnName("price");
            entity.Property(e => e.PublishedYear).HasColumnName("published_year");
            entity.Property(e => e.PublisherId).HasColumnName("publisher_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UrlImage).HasColumnName("url_image");
            entity.Property(e => e.ViewCount)
                .HasDefaultValue(0)
                .HasColumnName("view_count");

            entity.HasOne(d => d.Admin).WithMany(p => p.Books)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_books_admin");

            entity.HasOne(d => d.Author).WithMany(p => p.Books)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_books_author");

            entity.HasOne(d => d.Publisher).WithMany(p => p.Books)
                .HasForeignKey(d => d.PublisherId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_books_publisher");
        });

        modelBuilder.Entity<FavoriteBook>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.BookId }).HasName("favorite_books_pkey");

            entity.ToTable("favorite_books");

            entity.HasIndex(e => e.BookId, "idx_favorite_books_book_id");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Book).WithMany(p => p.FavoriteBooks)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("favorite_books_book_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.FavoriteBooks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("favorite_books_user_id_fkey");
        });

        modelBuilder.Entity<Publisher>(entity =>
        {
            entity.HasKey(e => e.PublisherId).HasName("publishers_pkey");

            entity.ToTable("publishers");

            entity.Property(e => e.PublisherId).HasColumnName("publisher_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.PublisherName)
                .HasMaxLength(255)
                .HasColumnName("publisher_name");
            entity.Property(e => e.Website)
                .HasMaxLength(255)
                .HasColumnName("website");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("reviews_pkey");

            entity.ToTable("reviews");

            entity.HasIndex(e => e.BookId, "idx_reviews_book_id");

            entity.HasIndex(e => e.ParentReviewId, "idx_reviews_parent_review_id");

            entity.HasIndex(e => e.UserId, "idx_reviews_user_id");

            entity.HasIndex(e => new { e.BookId, e.UserId }, "unique_main_review_per_user_per_book")
                .IsUnique()
                .HasFilter("(parent_review_id IS NULL)");

            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.CommentText).HasColumnName("comment_text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DislikeCount)
                .HasDefaultValue(0)
                .HasColumnName("dislike_count");
            entity.Property(e => e.LikeCount)
                .HasDefaultValue(0)
                .HasColumnName("like_count");
            entity.Property(e => e.ParentReviewId).HasColumnName("parent_review_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Book).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("reviews_book_id_fkey");

            entity.HasOne(d => d.ParentReview).WithMany(p => p.InverseParentReview)
                .HasForeignKey(d => d.ParentReviewId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("reviews_parent_review_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("reviews_user_id_fkey");
        });

        modelBuilder.Entity<ReviewVote>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ReviewId }).HasName("review_votes_pkey");

            entity.ToTable("review_votes");

            entity.HasIndex(e => e.ReviewId, "idx_review_votes_review_id");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.VoteType).HasColumnName("vote_type");

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewVotes)
                .HasForeignKey(d => d.ReviewId)
                .HasConstraintName("review_votes_review_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ReviewVotes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("review_votes_user_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "roles_role_name_key").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<SearchLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("search_logs_pkey");

            entity.ToTable("search_logs");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.SearchText).HasColumnName("search_text");
            entity.Property(e => e.SearchTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("search_time");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.SearchLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("search_logs_user_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Username, "idx_users_username");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Username).HasColumnName("username");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("user_roles_role_id_fkey"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("user_roles_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("user_roles_pkey");
                        j.ToTable("user_roles");
                        j.HasIndex(new[] { "RoleId" }, "idx_user_roles_role_id");
                        j.IndexerProperty<long>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<int>("RoleId").HasColumnName("role_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
