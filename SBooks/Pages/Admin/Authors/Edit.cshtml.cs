using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Authors
{
    public class EditModel : AdminPageModel
    {
        public EditModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty]
        public AuthorInputModel Author { get; set; } = new();

        public class AuthorInputModel
        {
            public long AuthorId { get; set; }

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

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors.FirstOrDefaultAsync(m => m.AuthorId == id);
            if (author == null)
            {
                return NotFound();
            }

            Author = new AuthorInputModel
            {
                AuthorId = author.AuthorId,
                AuthorName = author.AuthorName,
                Email = author.Email,
                BirthDate = author.BirthDate,
                Biography = author.Biography,
                Sex = author.Sex
            };

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
                var authorToUpdate = await _context.Authors.FirstOrDefaultAsync(a => a.AuthorId == Author.AuthorId);
                if (authorToUpdate == null)
                {
                    return NotFound();
                }

                // Kiểm tra trùng email nếu có (ngoại trừ chính tác giả này)
                if (!string.IsNullOrEmpty(Author.Email))
                {
                    var existingAuthor = await _context.Authors
                        .FirstOrDefaultAsync(a => a.Email == Author.Email && a.AuthorId != Author.AuthorId);

                    if (existingAuthor != null)
                    {
                        ModelState.AddModelError("Author.Email", "Email này đã được sử dụng bởi tác giả khác.");
                        return Page();
                    }
                }

                // Cập nhật thông tin
                authorToUpdate.AuthorName = Author.AuthorName;
                authorToUpdate.Email = string.IsNullOrWhiteSpace(Author.Email) ? null : Author.Email;
                authorToUpdate.BirthDate = Author.BirthDate;
                authorToUpdate.Biography = string.IsNullOrWhiteSpace(Author.Biography) ? null : Author.Biography;
                authorToUpdate.Sex = string.IsNullOrWhiteSpace(Author.Sex) ? null : Author.Sex;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật thông tin tác giả thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra khi cập nhật tác giả: {ex.Message}");
                return Page();
            }
        }
    }
}