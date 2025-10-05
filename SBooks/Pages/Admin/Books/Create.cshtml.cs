using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using System.Security.Claims;

namespace SBooks.Pages.Admin.Books
{
    public class CreateModel : AdminPageModel
    {
        public CreateModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty]
        public BookInputModel Book { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem> Authors { get; set; } = new();
        public List<SelectListItem> Publishers { get; set; } = new();

        public class BookInputModel
        {
            [Required(ErrorMessage = "Tiêu đề sách là bắt buộc")]
            [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
            public string Title { get; set; } = string.Empty;

            [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
            public string? Description { get; set; }

            [Range(1000, 3000, ErrorMessage = "Năm xuất bản phải từ 1000 đến 3000")]
            public int? PublishedYear { get; set; }

            [StringLength(100, ErrorMessage = "Thể loại không được vượt quá 100 ký tự")]
            public string? Genre { get; set; }

            [Range(0, 99999999, ErrorMessage = "Giá phải từ 0 đến 99,999,999")]
            public decimal? Price { get; set; }

            public long? AuthorId { get; set; }

            public long? PublisherId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDropdownData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownData();
                return Page();
            }

            try
            {
                // Tạo đối tượng Book mới
                var book = new Book
                {
                    Title = Book.Title,
                    Description = Book.Description,
                    PublishedYear = Book.PublishedYear,
                    Genre = Book.Genre,
                    Price = Book.Price,
                    AuthorId = Book.AuthorId,
                    PublisherId = Book.PublisherId,
                    AdminId = GetCurrentUserId(),
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Xử lý upload ảnh
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var imageUrl = await SaveImageAsync(ImageFile);
                    book.UrlImage = imageUrl;
                }

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm sách mới thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra khi thêm sách: {ex.Message}");
                await LoadDropdownData();
                return Page();
            }
        }

        private async Task LoadDropdownData()
        {
            // Load Authors
            var authors = await _context.Authors
                .OrderBy(a => a.AuthorName)
                .Select(a => new { a.AuthorId, a.AuthorName })
                .ToListAsync();

            Authors = authors.Select(a => new SelectListItem
            {
                Value = a.AuthorId.ToString(),
                Text = a.AuthorName
            }).ToList();
            Authors.Insert(0, new SelectListItem { Value = "", Text = "-- Chọn tác giả --" });

            // Load Publishers
            var publishers = await _context.Publishers
                .OrderBy(p => p.PublisherName)
                .Select(p => new { p.PublisherId, p.PublisherName })
                .ToListAsync();

            Publishers = publishers.Select(p => new SelectListItem
            {
                Value = p.PublisherId.ToString(),
                Text = p.PublisherName
            }).ToList();
            Publishers.Insert(0, new SelectListItem { Value = "", Text = "-- Chọn nhà xuất bản --" });
        }

        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return null;
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            try
            {
                // Tạo thư mục lưu ảnh nếu chưa có
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "books");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Tạo tên file unique
                var fileExtension = Path.GetExtension(imageFile.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Trả về đường dẫn tương đối
                return $"/images/books/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lưu ảnh: {ex.Message}");
            }
        }
    }
}