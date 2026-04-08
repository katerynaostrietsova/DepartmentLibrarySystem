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
    public class CopiesController : Controller
    {
        private readonly LibraryDbContext _context;

        public CopiesController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Copies
        public async Task<IActionResult> Index()
        {
            var copies = await _context.Copies
                .Include(c => c.Publication)
                .ToListAsync();

            var availableCount = copies.Count(c => c.IsAvailable == true);
            var unavailableCount = copies.Count(c => c.IsAvailable == false);

            ViewBag.CopyStatusLabels = new List<string> { "Доступні", "Недоступні" };
            ViewBag.CopyStatusCounts = new List<int> { availableCount, unavailableCount };

            return View(copies);
        }

        public async Task<IActionResult> ExportToExcel()
        {
            var copies = await _context.Copies
                .Include(c => c.Publication)
                .OrderBy(c => c.InventoryNumber)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Copies");

            worksheet.Cell(1, 1).Value = "Інвентарний номер";
            worksheet.Cell(1, 2).Value = "Дата надходження";
            worksheet.Cell(1, 3).Value = "Доступність";
            worksheet.Cell(1, 4).Value = "Видання";

            for (int i = 0; i < copies.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = copies[i].InventoryNumber;
                worksheet.Cell(i + 2, 2).Value = copies[i].AcquisitionDate?.ToString("yyyy-MM-dd");
                worksheet.Cell(i + 2, 3).Value = copies[i].IsAvailable == true ? "Так" : "Ні";
                worksheet.Cell(i + 2, 4).Value = copies[i].Publication?.Title;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Copies.xlsx");
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
                var inventoryNumber = row.Cell(1).GetString().Trim();
                var publicationTitle = row.Cell(4).GetString().Trim();

                if (string.IsNullOrWhiteSpace(inventoryNumber) || string.IsNullOrWhiteSpace(publicationTitle))
                    continue;

                DateOnly? acquisitionDate = null;
                var dateCell = row.Cell(2);

                if (dateCell.DataType == XLDataType.DateTime)
                {
                    acquisitionDate = DateOnly.FromDateTime(dateCell.GetDateTime());
                }
                else
                {
                    var acquisitionDateText = dateCell.GetString().Trim();

                    if (!string.IsNullOrWhiteSpace(acquisitionDateText) &&
                        DateOnly.TryParseExact(acquisitionDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    {
                        acquisitionDate = parsedDate;
                    }
                }

                bool? isAvailable = null;
                var isAvailableText = row.Cell(3).GetString().Trim().ToLower();

                if (isAvailableText == "так")
                    isAvailable = true;
                else if (isAvailableText == "ні")
                    isAvailable = false;

                var publication = await _context.Publications
                    .FirstOrDefaultAsync(p => p.Title == publicationTitle);

                if (publication == null)
                    continue;

                var existingCopy = await _context.Copies
                    .FirstOrDefaultAsync(c => c.InventoryNumber == inventoryNumber);

                if (existingCopy == null)
                {
                    _context.Copies.Add(new Copy
                    {
                        InventoryNumber = inventoryNumber,
                        AcquisitionDate = acquisitionDate,
                        IsAvailable = isAvailable,
                        PublicationId = publication.PublicationId
                    });
                }
                else
                {
                    existingCopy.AcquisitionDate = acquisitionDate;
                    existingCopy.IsAvailable = isAvailable;
                    existingCopy.PublicationId = publication.PublicationId;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Імпорт Excel виконано успішно.";
            return RedirectToAction(nameof(Index));
        }


        // GET: Copies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var copy = await _context.Copies
    .Include(c => c.Publication)
    .Include(c => c.Borrowings)
        .ThenInclude(b => b.Reader)
    .FirstOrDefaultAsync(m => m.CopyId == id);

            if (copy == null)
            {
                return NotFound();
            }

            return View(copy);
        }

        // GET: Copies/Create
        public IActionResult Create()
        {
            ViewData["PublicationId"] = new SelectList(_context.Publications, "PublicationId", "Title");
            return View();
        }

        // POST: Copies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CopyId,PublicationId,InventoryNumber,AcquisitionDate,IsAvailable")] Copy copy)
        {
            if (ModelState.IsValid)
            {
                _context.Add(copy);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["PublicationId"] = new SelectList(_context.Publications, "PublicationId", "Title", copy.PublicationId);
            return View(copy);
        }

        // GET: Copies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var copy = await _context.Copies.FindAsync(id);
            if (copy == null)
            {
                return NotFound();
            }
            ViewData["PublicationId"] = new SelectList(_context.Publications, "PublicationId", "Title", copy.PublicationId);
            return View(copy);
        }

        // POST: Copies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CopyId,PublicationId,InventoryNumber,AcquisitionDate,IsAvailable")] Copy copy)
        {
            if (id != copy.CopyId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(copy);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CopyExists(copy.CopyId))
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
            ViewData["PublicationId"] = new SelectList(_context.Publications, "PublicationId", "Title", copy.PublicationId);
            return View(copy);
        }

        // GET: Copies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var copy = await _context.Copies
    .Include(c => c.Publication)
    .FirstOrDefaultAsync(m => m.CopyId == id);
            if (copy == null)
            {
                return NotFound();
            }

            return View(copy);
        }

        // POST: Copies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var copy = await _context.Copies.FindAsync(id);
            if (copy != null)
            {
                _context.Copies.Remove(copy);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CopyExists(int id)
        {
            return _context.Copies.Any(e => e.CopyId == id);
        }
    }
}
