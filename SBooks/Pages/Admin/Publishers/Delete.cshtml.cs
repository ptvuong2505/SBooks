using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Publishers
{
    public class DeleteModel : AdminPageModel
    {
        public DeleteModel(SbooksContext context) : base(context)
        {
        }

        [BindProperty]
        public Publisher Publisher { get; set; } = default!;

        public int BookCount { get; set; }
        public bool HasDependencies { get; set; }
        public IList<Book> SampleBooks { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publisher = await _context.Publishers
                .Include(p => p.Books)
                    .ThenInclude(b => b.Author)
                .FirstOrDefaultAsync(m => m.PublisherId == id);

            if (publisher == null)
            {
                return NotFound();
            }

            Publisher = publisher;
            BookCount = publisher.Books.Count;
            HasDependencies = BookCount > 0;

            // Lấy một số sách mẫu để hiển thị
            SampleBooks = publisher.Books.Take(5).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
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

            try
            {
                // Kiểm tra lại các ràng buộc trước khi xóa
                if (publisher.Books.Any())
                {
                    TempData["ErrorMessage"] = $"Không thể xóa nhà xuất bản '{publisher.PublisherName}' vì có {publisher.Books.Count} sách liên quan. Hãy xóa hoặc chuyển đổi các sách trước.";
                    return RedirectToPage("./Index");
                }

                // Xóa publisher
                _context.Publishers.Remove(publisher);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Xóa nhà xuất bản '{publisher.PublisherName}' thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa nhà xuất bản: " + ex.Message;
                return RedirectToPage("./Index");
            }
        }
    }
}