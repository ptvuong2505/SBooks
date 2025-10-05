using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

namespace SBooks.Pages.Admin.Books
{
    public class IndexModel : AdminPageModel
    {
        public IndexModel(SbooksContext context) : base(context)
        {
        }

        public IList<Book> Books { get; set; } = default!;

        // Search Properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTitle { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchDescription { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? AuthorId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? PublisherId { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<string> SelectedGenres { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? MinYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? MaxYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MinPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? CreatedFromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? CreatedToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "CreatedAt";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "desc";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        // Pagination properties
        public int CurrentPage => PageNumber;
        public int TotalPages => (int)Math.Ceiling((double)FilteredBooks / PageSize);
        public int PageSize => 10;

        // Dropdown Data
        public List<SelectListItem> Authors { get; set; } = new();
        public List<SelectListItem> Publishers { get; set; } = new();
        public List<string> AvailableGenres { get; set; } = new();

        // Stats
        public int TotalBooks { get; set; }
        public int FilteredBooks { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDropdownData();
            await LoadBooks();
        }

        private async Task LoadDropdownData()
        {
            // Load Authors for dropdown
            var authors = await _context.Authors
                .OrderBy(a => a.AuthorName)
                .Select(a => new { a.AuthorId, a.AuthorName })
                .ToListAsync();

            Authors = authors.Select(a => new SelectListItem
            {
                Value = a.AuthorId.ToString(),
                Text = a.AuthorName,
                Selected = AuthorId == a.AuthorId
            }).ToList();
            Authors.Insert(0, new SelectListItem { Value = "", Text = "-- Tất cả tác giả --" });

            // Load Publishers for dropdown
            var publishers = await _context.Publishers
                .OrderBy(p => p.PublisherName)
                .Select(p => new { p.PublisherId, p.PublisherName })
                .ToListAsync();

            Publishers = publishers.Select(p => new SelectListItem
            {
                Value = p.PublisherId.ToString(),
                Text = p.PublisherName,
                Selected = PublisherId == p.PublisherId
            }).ToList();
            Publishers.Insert(0, new SelectListItem { Value = "", Text = "-- Tất cả nhà xuất bản --" });

            // Load Available Genres
            AvailableGenres = await _context.Books
                .Where(b => !string.IsNullOrEmpty(b.Genre))
                .Select(b => b.Genre!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();
        }

        private async Task LoadBooks()
        {
            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Admin)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(SearchTitle))
            {
                query = query.Where(b => b.Title.Contains(SearchTitle));
            }

            if (!string.IsNullOrEmpty(SearchDescription))
            {
                query = query.Where(b => b.Description != null && b.Description.ToLower().Contains(SearchDescription.ToLower()));
            }

            if (AuthorId.HasValue && AuthorId > 0)
            {
                query = query.Where(b => b.AuthorId == AuthorId.Value);
            }

            if (PublisherId.HasValue && PublisherId > 0)
            {
                query = query.Where(b => b.PublisherId == PublisherId.Value);
            }

            if (SelectedGenres.Any())
            {
                query = query.Where(b => b.Genre != null && SelectedGenres.Contains(b.Genre));
            }

            if (MinYear.HasValue)
            {
                query = query.Where(b => b.PublishedYear >= MinYear.Value);
            }

            if (MaxYear.HasValue)
            {
                query = query.Where(b => b.PublishedYear <= MaxYear.Value);
            }

            if (MinPrice.HasValue)
            {
                query = query.Where(b => b.Price >= MinPrice.Value);
            }

            if (MaxPrice.HasValue)
            {
                query = query.Where(b => b.Price <= MaxPrice.Value);
            }

            if (CreatedFromDate.HasValue)
            {
                var fromUtc = CreatedFromDate.Value.ToUniversalTime();
                query = query.Where(b => b.CreatedAt >= fromUtc);
            }

            if (CreatedToDate.HasValue)
            {
                var toUtc = CreatedToDate.Value.Date.AddDays(1).ToUniversalTime().AddTicks(-1);
                query = query.Where(b => b.CreatedAt < toUtc);
            }

            // Get total counts
            TotalBooks = await _context.Books.CountAsync();
            FilteredBooks = await query.CountAsync();

            // Apply sorting
            query = SortBy.ToLower() switch
            {
                "title" => SortDirection == "asc" ? query.OrderBy(b => b.Title) : query.OrderByDescending(b => b.Title),
                "author" => SortDirection == "asc" ? query.OrderBy(b => b.Author!.AuthorName) : query.OrderByDescending(b => b.Author!.AuthorName),
                "publisher" => SortDirection == "asc" ? query.OrderBy(b => b.Publisher!.PublisherName) : query.OrderByDescending(b => b.Publisher!.PublisherName),
                "genre" => SortDirection == "asc" ? query.OrderBy(b => b.Genre) : query.OrderByDescending(b => b.Genre),
                "price" => SortDirection == "asc" ? query.OrderBy(b => b.Price) : query.OrderByDescending(b => b.Price),
                "publishedyear" => SortDirection == "asc" ? query.OrderBy(b => b.PublishedYear) : query.OrderByDescending(b => b.PublishedYear),
                "viewcount" => SortDirection == "asc" ? query.OrderBy(b => b.ViewCount) : query.OrderByDescending(b => b.ViewCount),
                "createdat" => SortDirection == "asc" ? query.OrderBy(b => b.CreatedAt) : query.OrderByDescending(b => b.CreatedAt),
                _ => query.OrderByDescending(b => b.CreatedAt)
            };

            // Apply pagination
            var skip = (PageNumber - 1) * PageSize;
            Books = await query.Skip(skip).Take(PageSize).ToListAsync();
        }

        public IActionResult OnPostClearFilters()
        {
            return RedirectToPage();
        }

        #region Export Excel

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            await LoadBooks();
            if (Books == null)
                return Page();

            // Implement Excel export logic here
            using (var workBook = new XLWorkbook())
            {
                var ws = workBook.Worksheets.Add("Books");
                ws.Cell(1, 1).Value = "ID";
                ws.Cell(1, 2).Value = "Title";
                ws.Cell(1, 3).Value = "Author";
                ws.Cell(1, 3).Value = "Publisher";
                ws.Cell(1, 4).Value = "Description";
                ws.Cell(1, 6).Value = "Genre";
                ws.Cell(1, 7).Value = "Price";
                ws.Cell(1, 8).Value = "Published Year";

                int row = 2;
                foreach (var book in Books)
                {
                    ws.Cell(row, 1).Value = book.BookId;
                    ws.Cell(row, 2).Value = book.Title;
                    ws.Cell(row, 3).Value = book.Author?.AuthorName;
                    ws.Cell(row, 4).Value = book.Publisher?.PublisherName;
                    ws.Cell(row, 5).Value = book.Description;
                    ws.Cell(row, 6).Value = book.Genre;
                    ws.Cell(row, 7).Value = book.Price;
                    ws.Cell(row, 8).Value = book.PublishedYear;
                    row++;
                }
                ws.Columns().AdjustToContents();
                ws.Row(1).Style.Font.Bold = true;

                using var stream = new MemoryStream();
                workBook.SaveAs(stream);
                var content = stream.ToArray();

                // 3️⃣ Trả file Excel về trình duyệt
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BooksReport.xlsx");
            }
        }
        #endregion


        #region Export Csv

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            await LoadBooks();
            if (Books == null)
                return Page();

            using var ms = new MemoryStream();
            using (var writer = new StreamWriter(ms))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(Books.Select(b => new
                {
                    b.BookId,
                    b.Title,
                    Author = b.Author?.AuthorName,
                    Publisher = b.Publisher?.PublisherName,
                    b.Description,
                    b.Genre,
                    b.Price,
                    b.PublishedYear
                }));
                writer.Flush();
                var result = ms.ToArray();

                return File(result, "text/csv", "BooksReport.csv");
            }
        }

        #endregion


        #region GetSortUrl

        // Thêm method helper để tạo sort URL
        public string GetSortUrl(string sortField)
        {
            var queryParams = new List<string>();

            // Add all current search parameters except sortBy and sortDirection
            if (!string.IsNullOrEmpty(SearchTitle))
                queryParams.Add($"SearchTitle={Uri.EscapeDataString(SearchTitle)}");

            if (!string.IsNullOrEmpty(SearchDescription))
                queryParams.Add($"SearchDescription={Uri.EscapeDataString(SearchDescription)}");

            if (AuthorId.HasValue && AuthorId > 0)
                queryParams.Add($"AuthorId={AuthorId}");

            if (PublisherId.HasValue && PublisherId > 0)
                queryParams.Add($"PublisherId={PublisherId}");

            if (SelectedGenres.Any())
            {
                foreach (var genre in SelectedGenres)
                    queryParams.Add($"SelectedGenres={Uri.EscapeDataString(genre)}");
            }

            if (MinYear.HasValue)
                queryParams.Add($"MinYear={MinYear}");

            if (MaxYear.HasValue)
                queryParams.Add($"MaxYear={MaxYear}");

            if (MinPrice.HasValue)
                queryParams.Add($"MinPrice={MinPrice}");

            if (MaxPrice.HasValue)
                queryParams.Add($"MaxPrice={MaxPrice}");

            if (CreatedFromDate.HasValue)
                queryParams.Add($"CreatedFromDate={CreatedFromDate.Value:yyyy-MM-dd}");

            if (CreatedToDate.HasValue)
                queryParams.Add($"CreatedToDate={CreatedToDate.Value:yyyy-MM-dd}");

            // Add sort parameters
            queryParams.Add($"SortBy={sortField}");

            // Toggle sort direction if same field, otherwise default to desc
            var newDirection = (SortBy == sortField && SortDirection == "asc") ? "desc" : "asc";
            queryParams.Add($"SortDirection={newDirection}");

            // Reset to page 1 when sorting
            queryParams.Add("PageNumber=1");

            return "?" + string.Join("&", queryParams);
        }
        #endregion

    }
}