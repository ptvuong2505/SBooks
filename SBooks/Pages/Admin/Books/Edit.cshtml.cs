using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using System.Security.Claims;

namespace SBooks.Pages.Admin.Books
{
    public class EditModel : AdminPageModel
    {
        public EditModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty]
        public BookInputModel Book { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        [BindProperty]
        public bool KeepCurrentImage { get; set; } = true;

        public List<SelectListItem> Authors { get; set; } = new();
        public List<SelectListItem> Publishers { get; set; } = new();

        public string? CurrentImageUrl { get; set; }

        public class BookInputModel
        {
            public long BookId { get; set; }

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

            public string? UrlImage { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            Book = new BookInputModel
            {
                BookId = book.BookId,
                Title = book.Title,
                Description = book.Description,
                PublishedYear = book.PublishedYear,
                Genre = book.Genre,
                Price = book.Price,
                AuthorId = book.AuthorId,
                PublisherId = book.PublisherId,
                UrlImage = book.UrlImage
            };

            CurrentImageUrl = book.UrlImage;
            await LoadDropdownData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                CurrentImageUrl = Book.UrlImage;
                await LoadDropdownData();
                return Page();
            }

            try
            {
                var bookToUpdate = await _context.Books.FirstOrDefaultAsync(b => b.BookId == Book.BookId);
                if (bookToUpdate == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin cơ bản
                bookToUpdate.Title = Book.Title;
                bookToUpdate.Description = Book.Description;
                bookToUpdate.PublishedYear = Book.PublishedYear;
                bookToUpdate.Genre = Book.Genre;
                bookToUpdate.Price = Book.Price;
                bookToUpdate.AuthorId = Book.AuthorId;
                bookToUpdate.PublisherId = Book.PublisherId;
                bookToUpdate.UpdatedAt = DateTime.UtcNow;

                // Xử lý ảnh
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(bookToUpdate.UrlImage))
                    {
                        await DeleteOldImageAsync(bookToUpdate.UrlImage);
                    }

                    // Lưu ảnh mới
                    var newImageUrl = await SaveImageAsync(ImageFile);
                    bookToUpdate.UrlImage = newImageUrl;
                }
                else if (!KeepCurrentImage)
                {
                    // Người dùng chọn xóa ảnh hiện tại
                    if (!string.IsNullOrEmpty(bookToUpdate.UrlImage))
                    {
                        await DeleteOldImageAsync(bookToUpdate.UrlImage);
                        bookToUpdate.UrlImage = null;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật sách thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra khi cập nhật sách: {ex.Message}");
                CurrentImageUrl = Book.UrlImage;
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
                Text = a.AuthorName,
                Selected = Book.AuthorId == a.AuthorId
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
                Text = p.PublisherName,
                Selected = Book.PublisherId == p.PublisherId
            }).ToList();
            Publishers.Insert(0, new SelectListItem { Value = "", Text = "-- Chọn nhà xuất bản --" });
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

        private async Task DeleteOldImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/images/books/"))
                    return;

                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "books", fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception)
            {
                // Log the error but don't throw - we don't want to fail the update because of image deletion
            }
        }
    }
}