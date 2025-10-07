using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using System.ComponentModel.DataAnnotations;

namespace SBooks.Pages.Admin.Publishers
{
    public class CreateModel : AdminPageModel
    {
        public CreateModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty]
        public PublisherInputModel Input { get; set; } = default!;

        public class PublisherInputModel
        {
            [Required(ErrorMessage = "Tên nhà xuất bản là bắt buộc")]
            [StringLength(200, ErrorMessage = "Tên nhà xuất bản không được quá 200 ký tự")]
            public string PublisherName { get; set; } = null!;

            [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
            public string? Address { get; set; }

            [StringLength(200, ErrorMessage = "Website không được quá 200 ký tự")]
            [Url(ErrorMessage = "Vui lòng nhập URL hợp lệ")]
            public string? Website { get; set; }
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

            // Kiểm tra tên nhà xuất bản trùng lặp
            if (await _context.Publishers.AnyAsync(p => p.PublisherName == Input.PublisherName))
            {
                ModelState.AddModelError("Input.PublisherName", "Tên nhà xuất bản đã tồn tại");
                return Page();
            }

            var publisher = new Publisher
            {
                PublisherName = Input.PublisherName,
                Address = Input.Address,
                Website = Input.Website
            };

            try
            {
                _context.Publishers.Add(publisher);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm nhà xuất bản thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm nhà xuất bản: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnGetCheckNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new JsonResult(new { available = true });
            }

            var exists = await _context.Publishers.AnyAsync(p => p.PublisherName == name);
            return new JsonResult(new { available = !exists });
        }
    }
}