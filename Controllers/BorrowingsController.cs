using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryWebMvc.Models;

namespace LibraryWebMvc.Controllers
{
    public class BorrowingsController : Controller
    {
        private readonly LibraryDbContext _context;

        public BorrowingsController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Borrowings
        public async Task<IActionResult> Index()
        {
            var borrowings = await _context.Borrowings
                .Include(b => b.Reader)
                .Include(b => b.Copy)
                .ToListAsync();

            var returnedCount = borrowings.Count(b => b.IsReturned == true);
            var notReturnedCount = borrowings.Count(b => b.IsReturned == false);

            ViewBag.BorrowingStatusLabels = new List<string> { "Повернені", "Неповернені" };
            ViewBag.BorrowingStatusCounts = new List<int> { returnedCount, notReturnedCount };

            return View(borrowings);
        }

        public async Task<IActionResult> ExportToExcel()
        {
            var borrowings = await _context.Borrowings
                .Include(b => b.Reader)
                .Include(b => b.Copy)
                .OrderBy(b => b.BorrowDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Borrowings");

            worksheet.Cell(1, 1).Value = "Читач";
            worksheet.Cell(1, 2).Value = "Інвентарний номер";
            worksheet.Cell(1, 3).Value = "Дата видачі";
            worksheet.Cell(1, 4).Value = "Термін повернення";
            worksheet.Cell(1, 5).Value = "Дата повернення";
            worksheet.Cell(1, 6).Value = "Повернено";

            for (int i = 0; i < borrowings.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = borrowings[i].Reader?.FullName;
                worksheet.Cell(i + 2, 2).Value = borrowings[i].Copy?.InventoryNumber;
                worksheet.Cell(i + 2, 3).Value = borrowings[i].BorrowDate.ToString("yyyy-MM-dd");
                worksheet.Cell(i + 2, 4).Value = borrowings[i].DueDate.ToString("yyyy-MM-dd");
                worksheet.Cell(i + 2, 5).Value = borrowings[i].ReturnDate.HasValue
                    ? borrowings[i].ReturnDate.Value.ToString("yyyy-MM-dd")
                    : "";
                worksheet.Cell(i + 2, 6).Value = borrowings[i].IsReturned == true ? "Так" : "Ні";
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Borrowings.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Оберіть Excel-файл для імпорту.";
                return RedirectToAction(nameof(Index));
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            var rows = worksheet.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                var readerName = row.Cell(1).GetString().Trim();
                var inventoryNumber = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(readerName) || string.IsNullOrWhiteSpace(inventoryNumber))
                    continue;

                var reader = await _context.Readers
                    .FirstOrDefaultAsync(r => r.FullName == readerName);

                if (reader == null)
                    continue;

                var copy = await _context.Copies
                    .FirstOrDefaultAsync(c => c.InventoryNumber == inventoryNumber);

                if (copy == null)
                    continue;

                DateOnly borrowDate;
                DateOnly dueDate;
                DateOnly? returnDate = null;

                var borrowDateCell = row.Cell(3);
                if (borrowDateCell.DataType == XLDataType.DateTime)
                {
                    borrowDate = DateOnly.FromDateTime(borrowDateCell.GetDateTime());
                }
                else
                {
                    var borrowDateText = borrowDateCell.GetString().Trim();
                    if (!DateOnly.TryParseExact(borrowDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out borrowDate))
                        continue;
                }

                var dueDateCell = row.Cell(4);
                if (dueDateCell.DataType == XLDataType.DateTime)
                {
                    dueDate = DateOnly.FromDateTime(dueDateCell.GetDateTime());
                }
                else
                {
                    var dueDateText = dueDateCell.GetString().Trim();
                    if (!DateOnly.TryParseExact(dueDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dueDate))
                        continue;
                }

                var returnDateCell = row.Cell(5);
                if (returnDateCell.DataType == XLDataType.DateTime)
                {
                    returnDate = DateOnly.FromDateTime(returnDateCell.GetDateTime());
                }
                else
                {
                    var returnDateText = returnDateCell.GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(returnDateText) &&
                        DateOnly.TryParseExact(returnDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedReturnDate))
                    {
                        returnDate = parsedReturnDate;
                    }
                }

                bool isReturned = false;
                var isReturnedText = row.Cell(6).GetString().Trim().ToLower();

                if (isReturnedText == "так")
                    isReturned = true;
                else if (isReturnedText == "ні")
                    isReturned = false;

                var existingBorrowing = await _context.Borrowings
                    .FirstOrDefaultAsync(b =>
                        b.ReaderId == reader.ReaderId &&
                        b.CopyId == copy.CopyId &&
                        b.BorrowDate == borrowDate);

                if (existingBorrowing == null)
                {
                    _context.Borrowings.Add(new Borrowing
                    {
                        ReaderId = reader.ReaderId,
                        CopyId = copy.CopyId,
                        BorrowDate = borrowDate,
                        DueDate = dueDate,
                        ReturnDate = returnDate,
                        IsReturned = isReturned
                    });
                }
                else
                {
                    existingBorrowing.DueDate = dueDate;
                    existingBorrowing.ReturnDate = returnDate;
                    existingBorrowing.IsReturned = isReturned;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Імпорт Excel виконано успішно.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Borrowings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrowing = await _context.Borrowings
                .Include(b => b.Reader)
                .Include(b => b.Copy)
                    .ThenInclude(c => c.Publication)
                .FirstOrDefaultAsync(m => m.BorrowingId == id);

            if (borrowing == null)
            {
                return NotFound();
            }

            return View(borrowing);
        }

        // GET: Borrowings/Create
        public IActionResult Create()
        {
            ViewData["ReaderId"] = new SelectList(_context.Readers, "ReaderId", "FullName");
            ViewData["CopyId"] = new SelectList(_context.Copies, "CopyId", "InventoryNumber");
            return View();
        }

        // POST: Borrowings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BorrowingId,ReaderId,CopyId,BorrowDate,DueDate,ReturnDate,IsReturned")] Borrowing borrowing)
        {
            ModelState.Remove("Reader");
            ModelState.Remove("Copy");

            if (ModelState.IsValid)
            {
                _context.Add(borrowing);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ReaderId"] = new SelectList(_context.Readers, "ReaderId", "FullName", borrowing.ReaderId);
            ViewData["CopyId"] = new SelectList(_context.Copies, "CopyId", "InventoryNumber", borrowing.CopyId);
            return View(borrowing);
        }

        // GET: Borrowings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrowing = await _context.Borrowings.FindAsync(id);
            if (borrowing == null)
            {
                return NotFound();
            }
            ViewData["ReaderId"] = new SelectList(_context.Readers, "ReaderId", "FullName", borrowing.ReaderId);
            ViewData["CopyId"] = new SelectList(_context.Copies, "CopyId", "InventoryNumber", borrowing.CopyId);
            return View(borrowing);
        }

        // POST: Borrowings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BorrowingId,ReaderId,CopyId,BorrowDate,DueDate,ReturnDate,IsReturned")] Borrowing borrowing)
        {

            if (id != borrowing.BorrowingId)
            {
                return NotFound();
            } 

            ModelState.Remove("Reader");
            ModelState.Remove("Copy");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(borrowing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BorrowingExists(borrowing.BorrowingId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReaderId"] = new SelectList(_context.Readers, "ReaderId", "FullName", borrowing.ReaderId);
            ViewData["CopyId"] = new SelectList(_context.Copies, "CopyId", "InventoryNumber", borrowing.CopyId);
            return View(borrowing);
        }

        // GET: Borrowings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrowing = await _context.Borrowings
    .Include(b => b.Reader)
    .Include(b => b.Copy)
    .FirstOrDefaultAsync(m => m.BorrowingId == id);
            if (borrowing == null)
            {
                return NotFound();
            }

            return View(borrowing);
        }

        // POST: Borrowings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var borrowing = await _context.Borrowings.FindAsync(id);
            if (borrowing != null)
            {
                _context.Borrowings.Remove(borrowing);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BorrowingExists(int id)
        {
            return _context.Borrowings.Any(e => e.BorrowingId == id);
        }
    }
}
