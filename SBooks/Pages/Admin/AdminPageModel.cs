using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;

namespace SBooks.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public abstract class AdminPageModel : PageModel
    {
        protected readonly SbooksContext _context;

        protected AdminPageModel(SbooksContext context)
        {
            _context = context;
        }

        // public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        // {
        //     await LoadSidebarData();
        //     await next();
        // }

        protected virtual async Task LoadSidebarData()
        {
            try
            {
                ViewData["TotalBooks"] = await _context.Books.CountAsync();
                ViewData["TotalAuthors"] = await _context.Authors.CountAsync();
                ViewData["TotalUsers"] = await _context.Users.CountAsync();
                ViewData["TotalReviews"] = await _context.Reviews.CountAsync();
            }
            catch
            {
                // Nếu có lỗi, không hiển thị số liệu
                ViewData["TotalBooks"] = "";
                ViewData["TotalAuthors"] = "";
                ViewData["TotalUsers"] = "";
                ViewData["TotalReviews"] = "";
            }
        }

        protected void SetBreadcrumb(params (string text, string? url)[] breadcrumbItems)
        {
            var breadcrumbHtml = "";
            for (int i = 0; i < breadcrumbItems.Length; i++)
            {
                var item = breadcrumbItems[i];
                if (i == breadcrumbItems.Length - 1)
                {
                    // Last item - active
                    breadcrumbHtml += $"<li class=\"breadcrumb-item active\" aria-current=\"page\">{item.text}</li>";
                }
                else
                {
                    // Other items - with link
                    breadcrumbHtml += $"<li class=\"breadcrumb-item\"><a href=\"{item.url}\">{item.text}</a></li>";
                }
            }
            ViewData["Breadcrumb"] = breadcrumbHtml;
        }
    }
}