using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;
using System.ComponentModel.DataAnnotations;

namespace SBooks.Pages.Admin.Publishers
{
    public class EditModel : AdminPageModel
    {
        public EditModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty]
        public PublisherInputModel Input { get; set; } = default!;

        public Publisher Publisher { get; set; } = default!;

        public class PublisherInputModel
        {
            public long PublisherId { get; set; }

            [Required(ErrorMessage = "Tên nhà xuất bản là bắt buộc")]
            [StringLength(200, ErrorMessage = "Tên nhà xuất bản không được quá 200 ký tự")]
            public string PublisherName { get; set; } = null!;

            [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
            public string? Address { get; set; }

            [StringLength(200, ErrorMessage = "Website không được quá 200 ký tự")]
            [Url(ErrorMessage = "Vui lòng nhập URL hợp lệ")]
            public string? Website { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publisher = await _context.Publishers
                .Include(p => p.Books)
                .FirstOrDefaultAsync(m => m.PublisherId == id);

            if (publisher == null)
            {
                return NotFound();
            }

            Publisher = publisher;
            Input = new PublisherInputModel
            {
                PublisherId = publisher.PublisherId,
                PublisherName = publisher.PublisherName,
                Address = publisher.Address,
                Website = publisher.Website
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Publisher = await _context.Publishers
                    .Include(p => p.Books)
                    .FirstOrDefaultAsync(m => m.PublisherId == Input.PublisherId);
                return Page();
            }

            var publisher = await _context.Publishers
                .FirstOrDefaultAsync(m => m.PublisherId == Input.PublisherId);

            if (publisher == null)
            {
                return NotFound();
            }

            // Kiểm tra tên nhà xuất bản trùng lặp (trừ chính publisher này)
            if (await _context.Publishers.AnyAsync(p => p.PublisherName == Input.PublisherName && p.PublisherId != Input.PublisherId))
            {
                ModelState.AddModelError("Input.PublisherName", "Tên nhà xuất bản đã tồn tại");
                Publisher = await _context.Publishers
                    .Include(p => p.Books)
                    .FirstOrDefaultAsync(m => m.PublisherId == Input.PublisherId);
                return Page();
            }

            try
            {
                publisher.PublisherName = Input.PublisherName;
                publisher.Address = Input.Address;
                publisher.Website = Input.Website;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật nhà xuất bản thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật nhà xuất bản: " + ex.Message);
                Publisher = await _context.Publishers
                    .Include(p => p.Books)
                    .FirstOrDefaultAsync(m => m.PublisherId == Input.PublisherId);
                return Page();
            }
        }

        public async Task<IActionResult> OnGetCheckNameAsync(string name, long publisherId)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new JsonResult(new { available = true });
            }

            var exists = await _context.Publishers.AnyAsync(p => p.PublisherName == name && p.PublisherId != publisherId);
            return new JsonResult(new { available = !exists });
        }
    }
}