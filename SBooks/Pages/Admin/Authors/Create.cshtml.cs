using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Authors
{
    public class CreateModel : AdminPageModel
    {
        public CreateModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty]
        public AuthorInputModel Author { get; set; } = new();

        public class AuthorInputModel
        {
            [Required(ErrorMessage = "Tên tác giả là bắt buộc")]
            [StringLength(255, ErrorMessage = "Tên tác giả không được vượt quá 255 ký tự")]
            public string AuthorName { get; set; } = string.Empty;

            [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
            [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
            public string? Email { get; set; }

            [DataType(DataType.Date)]
            public DateOnly? BirthDate { get; set; }

            [StringLength(2000, ErrorMessage = "Tiểu sử không được vượt quá 2000 ký tự")]
            public string? Biography { get; set; }

            [StringLength(20, ErrorMessage = "Giới tính không được vượt quá 20 ký tự")]
            public string? Sex { get; set; }
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Kiểm tra trùng email nếu có
                if (!string.IsNullOrEmpty(Author.Email))
                {
                    var existingAuthor = await _context.Authors
                        .FirstOrDefaultAsync(a => a.Email == Author.Email);

                    if (existingAuthor != null)
                    {
                        ModelState.AddModelError("Author.Email", "Email này đã được sử dụng bởi tác giả khác.");
                        return Page();
                    }
                }

                // Tạo đối tượng Author mới
                var author = new Author
                {
                    AuthorName = Author.AuthorName,
                    Email = string.IsNullOrWhiteSpace(Author.Email) ? null : Author.Email,
                    BirthDate = Author.BirthDate,
                    Biography = string.IsNullOrWhiteSpace(Author.Biography) ? null : Author.Biography,
                    Sex = string.IsNullOrWhiteSpace(Author.Sex) ? null : Author.Sex
                };

                _context.Authors.Add(author);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm tác giả mới thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra khi thêm tác giả: {ex.Message}");
                return Page();
            }
        }
    }
}