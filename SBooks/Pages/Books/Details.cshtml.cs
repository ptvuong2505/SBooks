using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using SBooks.Models.DTOs;
using SBooks.Extensions;

namespace SBooks.Pages.Books
{
    public class DetailsModel : PageModel
    {
        private readonly SbooksContext _context;

        public DetailsModel(SbooksContext context)
        {
            _context = context;
        }

        public Book? Book { get; set; }
        public List<BookCardDto> RelatedBooks { get; set; } = new();
        public List<Review> MainReviews { get; set; } = new();
        public bool IsFavorited { get; set; }
        public bool CanReview { get; set; }
        public Review? UserReview { get; set; }
        public double AverageRating { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();

        [BindProperty]
        public int Rating { get; set; }

        [BindProperty]
        public string? CommentText { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Reviews)
                    .ThenInclude(r => r.User)
                .Include(b => b.Reviews)
                    .ThenInclude(r => r.InverseParentReview)
                        .ThenInclude(r => r.User)
                .Include(b => b.FavoriteBooks)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (Book == null)
            {
                return NotFound();
            }

            // Tăng view count
            Book.ViewCount = (Book.ViewCount ?? 0) + 1;
            await _context.SaveChangesAsync();

            await LoadBookDataAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Validate rating
            if (Rating < 1 || Rating > 5)
            {
                ModelState.AddModelError("Rating", "Đánh giá phải từ 1 đến 5 sao");
            }

            if (!ModelState.IsValid)
            {
                await OnGetAsync(id);
                return Page();
            }

            // Check if user already reviewed this book
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookId == id && r.UserId == userId && r.ParentReviewId == null);

            if (existingReview != null)
            {
                // Update existing review
                existingReview.Rating = (short)Rating;
                existingReview.CommentText = CommentText;
                existingReview.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new review
                var review = new Review
                {
                    BookId = id,
                    UserId = userId.Value,
                    Rating = (short)Rating,
                    CommentText = CommentText,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(long bookId)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return new JsonResult(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này" });
            }

            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Không thể xác định người dùng" });
            }

            var existingFavorite = await _context.FavoriteBooks
                .FirstOrDefaultAsync(f => f.BookId == bookId && f.UserId == userId.Value);

            if (existingFavorite != null)
            {
                _context.FavoriteBooks.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, isFavorited = false });
            }
            else
            {
                var favorite = new FavoriteBook
                {
                    BookId = bookId,
                    UserId = userId.Value
                };

                _context.FavoriteBooks.Add(favorite);
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, isFavorited = true });
            }
        }

        public async Task<IActionResult> OnPostReplyReviewAsync(long parentReviewId, string commentText)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return new JsonResult(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này" });
            }

            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Không thể xác định người dùng" });
            }

            if (string.IsNullOrWhiteSpace(commentText))
            {
                return new JsonResult(new { success = false, message = "Nội dung phản hồi không được để trống" });
            }

            // Verify parent review exists
            var parentReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == parentReviewId);

            if (parentReview == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy bình luận gốc" });
            }

            var reply = new Review
            {
                BookId = parentReview.BookId,
                UserId = userId.Value,
                CommentText = commentText.Trim(),
                ParentReviewId = parentReviewId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(reply);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Phản hồi đã được gửi thành công" });
        }

        public async Task<IActionResult> OnPostLikeReviewAsync(long reviewId, bool isLike)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return new JsonResult(new { success = false, message = "Bạn cần đăng nhập" });
            }

            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Không thể xác định người dùng" });
            }

            var existingVote = await _context.ReviewVotes
                .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId.Value);

            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy đánh giá" });
            }

            var newVoteType = (short)(isLike ? 1 : -1);

            if (existingVote != null)
            {
                // Remove old vote counts
                if (existingVote.VoteType == 1)
                {
                    review.LikeCount = Math.Max(0, (review.LikeCount ?? 0) - 1);
                }
                else
                {
                    review.DislikeCount = Math.Max(0, (review.DislikeCount ?? 0) - 1);
                }

                if (existingVote.VoteType == newVoteType)
                {
                    // Remove vote if same type
                    _context.ReviewVotes.Remove(existingVote);
                }
                else
                {
                    // Change vote type
                    existingVote.VoteType = newVoteType;
                    existingVote.CreatedAt = DateTime.UtcNow;

                    // Add new vote count
                    if (newVoteType == 1)
                    {
                        review.LikeCount = (review.LikeCount ?? 0) + 1;
                    }
                    else
                    {
                        review.DislikeCount = (review.DislikeCount ?? 0) + 1;
                    }
                }
            }
            else
            {
                // Create new vote
                var vote = new ReviewVote
                {
                    ReviewId = reviewId,
                    UserId = userId.Value,
                    VoteType = newVoteType,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReviewVotes.Add(vote);

                // Add vote count
                if (newVoteType == 1)
                {
                    review.LikeCount = (review.LikeCount ?? 0) + 1;
                }
                else
                {
                    review.DislikeCount = (review.DislikeCount ?? 0) + 1;
                }
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                likeCount = review.LikeCount ?? 0,
                dislikeCount = review.DislikeCount ?? 0
            });
        }

        private async Task LoadBookDataAsync()
        {
            if (Book == null) return;

            var userId = User.GetUserId();

            // Check if favorited
            if (userId.HasValue)
            {
                IsFavorited = await _context.FavoriteBooks
                    .AnyAsync(f => f.BookId == Book.BookId && f.UserId == userId.Value);

                // Check if user can review (hasn't reviewed yet)
                CanReview = !await _context.Reviews
                    .AnyAsync(r => r.BookId == Book.BookId && r.UserId == userId.Value && r.ParentReviewId == null);

                // Get user's review if exists
                UserReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.BookId == Book.BookId && r.UserId == userId.Value && r.ParentReviewId == null);
            }

            // Calculate average rating and distribution
            var ratings = Book.Reviews
                .Where(r => r.ParentReviewId == null && r.Rating.HasValue)
                .Select(r => r.Rating!.Value)
                .ToList();

            AverageRating = ratings.Any() ? ratings.Select(r => (double)r).Average() : 0;

            RatingDistribution = Enumerable.Range(1, 5)
                .ToDictionary(i => i, i => ratings.Count(r => r == i));

            // Get main reviews (not replies)
            MainReviews = Book.Reviews
                .Where(r => r.ParentReviewId == null)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            // Load related books
            await LoadRelatedBooksAsync();
        }

        private async Task LoadRelatedBooksAsync()
        {
            if (Book == null) return;

            var userId = User.GetUserId();
            var relatedBooks = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Reviews)
                .Include(b => b.FavoriteBooks)
                .Where(b => b.BookId != Book.BookId &&
                           (b.AuthorId == Book.AuthorId ||
                            b.Genre == Book.Genre))
                .Take(8)
                .Select(b => new BookCardDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Description = b.Description,
                    PublishedYear = b.PublishedYear,
                    Genre = b.Genre,
                    Price = b.Price,
                    UrlImage = b.UrlImage,
                    AuthorName = b.Author != null ? b.Author.AuthorName : null,
                    AverageRating = b.Reviews
                        .Where(r => r.ParentReviewId == null && r.Rating.HasValue)
                        .Average(r => (double?)r.Rating) ?? 0,
                    ReviewCount = b.Reviews.Count(r => r.ParentReviewId == null),
                    FavoriteCount = b.FavoriteBooks.Count,
                    IsFavorited = userId.HasValue &&
                        b.FavoriteBooks.Any(f => f.UserId == userId.Value)
                })
                .ToListAsync();

            RelatedBooks = relatedBooks;
        }
    }
}
