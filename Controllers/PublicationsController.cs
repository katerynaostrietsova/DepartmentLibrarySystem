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
    public class PublicationsController : Controller
    {
        private readonly LibraryDbContext _context;

        public PublicationsController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Publications
        public async Task<IActionResult> Index()
        {
            var publications = await _context.Publications
                .Include(p => p.Publisher)
                .Include(p => p.PublicationType)
                .ToListAsync();

            var yearStats = publications
                .Where(p => p.Year.HasValue)
                .GroupBy(p => p.Year!.Value)
                .Select(g => new
                {
                    Year = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ToList();

            ViewBag.YearLabels = yearStats.Select(x => x.Year).ToList();
            ViewBag.YearCounts = yearStats.Select(x => x.Count).ToList();

            return View(publications);
        }

        public async Task<IActionResult> ExportToExcel()
        {
            var publications = await _context.Publications
                .Include(p => p.Publisher)
                .Include(p => p.PublicationType)
                .OrderBy(p => p.Title)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Publications");

            worksheet.Cell(1, 1).Value = "Назва";
            worksheet.Cell(1, 2).Value = "Анотація";
            worksheet.Cell(1, 3).Value = "Рік";
            worksheet.Cell(1, 4).Value = "Мова";
            worksheet.Cell(1, 5).Value = "Видавництво";
            worksheet.Cell(1, 6).Value = "Тип видання";

            for (int i = 0; i < publications.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = publications[i].Title;
                worksheet.Cell(i + 2, 2).Value = publications[i].Annotation;
                worksheet.Cell(i + 2, 3).Value = publications[i].Year;
                worksheet.Cell(i + 2, 4).Value = publications[i].Language;
                worksheet.Cell(i + 2, 5).Value = publications[i].Publisher?.Name;
                worksheet.Cell(i + 2, 6).Value = publications[i].PublicationType?.TypeName;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Publications.xlsx");
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
                var title = row.Cell(1).GetString().Trim();
                var annotation = row.Cell(2).GetString().Trim();
                var yearText = row.Cell(3).GetString().Trim();
                var language = row.Cell(4).GetString().Trim();
                var publisherName = row.Cell(5).GetString().Trim();
                var publicationTypeName = row.Cell(6).GetString().Trim();

                if (string.IsNullOrWhiteSpace(title) ||
                    string.IsNullOrWhiteSpace(publisherName) ||
                    string.IsNullOrWhiteSpace(publicationTypeName))
                    continue;

                int? year = null;
                if (!string.IsNullOrWhiteSpace(yearText) && int.TryParse(yearText, out int parsedYear))
                {
                    year = parsedYear;
                }

                var publisher = await _context.Publishers
                    .FirstOrDefaultAsync(p => p.Name == publisherName);

                if (publisher == null)
                    continue;

                var publicationType = await _context.PublicationTypes
                    .FirstOrDefaultAsync(pt => pt.TypeName == publicationTypeName);

                if (publicationType == null)
                    continue;

                var existingPublication = await _context.Publications
                    .FirstOrDefaultAsync(p => p.Title == title);

                if (existingPublication == null)
                {
                    _context.Publications.Add(new Publication
                    {
                        Title = title,
                        Annotation = annotation,
                        Year = year,
                        Language = language,
                        PublisherId = publisher.PublisherId,
                        PublicationTypeId = publicationType.PublicationTypeId
                    });
                }
                else
                {
                    existingPublication.Annotation = annotation;
                    existingPublication.Year = year;
                    existingPublication.Language = language;
                    existingPublication.PublisherId = publisher.PublisherId;
                    existingPublication.PublicationTypeId = publicationType.PublicationTypeId;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Імпорт Excel виконано успішно.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Publications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publication = await _context.Publications
                .Include(p => p.Publisher)
                .Include(p => p.PublicationType)
                .Include(p => p.Copies)
                .FirstOrDefaultAsync(m => m.PublicationId == id);

            if (publication == null)
            {
                return NotFound();
            }

            return View(publication);
        }

        // GET: Publications/Create
        public IActionResult Create()
        {
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "PublisherId", "Name");
            ViewData["PublicationTypeId"] = new SelectList(_context.PublicationTypes, "PublicationTypeId", "TypeName");
            ViewData["AuthorId"] = new SelectList(_context.Authors, "AuthorId", "FullName");
            return View();
        }

        // POST: Publications/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [Bind("PublicationId,Title,Annotation,Year,Language,PublisherId,PublicationTypeId")] Publication publication,
    int authorId)
        {
            if (authorId == 0)
            {
                ModelState.AddModelError("AuthorId", "Please select an author.");
            }

            ModelState.Remove("Publisher");
            ModelState.Remove("PublicationType");
            ModelState.Remove("Authors");

            if (ModelState.IsValid)
            {
                var author = await _context.Authors.FindAsync(authorId);

                if (author != null)
                {
                    publication.Authors.Add(author);
                }

                _context.Add(publication);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["PublisherId"] = new SelectList(_context.Publishers, "PublisherId", "Name", publication.PublisherId);
            ViewData["PublicationTypeId"] = new SelectList(_context.PublicationTypes, "PublicationTypeId", "TypeName", publication.PublicationTypeId);
            ViewData["AuthorId"] = new SelectList(_context.Authors, "AuthorId", "FullName", authorId);

            return View(publication);
        }

        // GET: Publications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publication = await _context.Publications.FindAsync(id);
            if (publication == null)
            {
                return NotFound();
            }
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "PublisherId", "Name", publication.PublisherId);
            ViewData["PublicationTypeId"] = new SelectList(_context.PublicationTypes, "PublicationTypeId", "TypeName", publication.PublicationTypeId);
            return View(publication);
        }

        // POST: Publications/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PublicationId,Title,Annotation,Year,Language,PublisherId,PublicationTypeId")] Publication publication)
        {
            if (id != publication.PublicationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(publication);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PublicationExists(publication.PublicationId))
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
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "PublisherId", "Name", publication.PublisherId);
            ViewData["PublicationTypeId"] = new SelectList(_context.PublicationTypes, "PublicationTypeId", "TypeName", publication.PublicationTypeId);
            return View(publication);
        }

        // GET: Publications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publication = await _context.Publications
    .Include(p => p.Publisher)
    .Include(p => p.PublicationType)
    .FirstOrDefaultAsync(m => m.PublicationId == id);

            if (publication == null)
            {
                return NotFound();
            }

            return View(publication);
        }

        // POST: Publications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var publication = await _context.Publications.FindAsync(id);
            if (publication != null)
            {
                _context.Publications.Remove(publication);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PublicationExists(int id)
        {
            return _context.Publications.Any(e => e.PublicationId == id);
        }
    }
}
