using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using System.IO;
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
    public class AuthorsController : Controller
    {
        private readonly LibraryDbContext _context;

        public AuthorsController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Authors
        public async Task<IActionResult> Index()
        {
            var authors = await _context.Authors.ToListAsync();

            var countryStats = authors
                .GroupBy(a => string.IsNullOrWhiteSpace(a.Country) ? "Невідомо" : a.Country.Trim())
                .Select(g => new
                {
                    Country = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Country)
                .ToList();

            ViewBag.CountryLabels = countryStats.Select(x => x.Country).ToList();
            ViewBag.CountryCounts = countryStats.Select(x => x.Count).ToList();

            return View(authors);
        }
        public async Task<IActionResult> ExportToExcel()
        {
            var authors = await _context.Authors
                .OrderBy(a => a.FullName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Authors");

            worksheet.Cell(1, 1).Value = "ПІБ";
            worksheet.Cell(1, 2).Value = "Рік народження";
            worksheet.Cell(1, 3).Value = "Країна";

            for (int i = 0; i < authors.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = authors[i].FullName;
                worksheet.Cell(i + 2, 2).Value = authors[i].BirthYear;
                worksheet.Cell(i + 2, 3).Value = authors[i].Country;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Authors.xlsx");
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
                var fullName = row.Cell(1).GetString().Trim();
                var birthYearText = row.Cell(2).GetString().Trim();
                var country = row.Cell(3).GetString().Trim();

                if (string.IsNullOrWhiteSpace(fullName))
                    continue;

                int? birthYear = null;
                if (int.TryParse(birthYearText, out int parsedYear))
                {
                    birthYear = parsedYear;
                }

                var existingAuthor = await _context.Authors
                    .FirstOrDefaultAsync(a => a.FullName == fullName && a.BirthYear == birthYear);

                if (existingAuthor == null)
                {
                    _context.Authors.Add(new Author
                    {
                        FullName = fullName,
                        BirthYear = birthYear,
                        Country = country
                    });
                }
                else
                {
                    existingAuthor.Country = country;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Імпорт Excel виконано успішно.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Authors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                .Include(a => a.Publications)
                    .ThenInclude(p => p.Publisher)
                .FirstOrDefaultAsync(m => m.AuthorId == id);

            if (author == null)
            {
                return NotFound();
            }

            return View(author);
        }

        // GET: Authors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Authors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AuthorId,FullName,BirthYear,Country")] Author author)
        {
            if (ModelState.IsValid)
            {
                _context.Add(author);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }

        // GET: Authors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound();
            }
            return View(author);
        }

        // POST: Authors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AuthorId,FullName,BirthYear,Country")] Author author)
        {
            if (id != author.AuthorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(author);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AuthorExists(author.AuthorId))
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
            return View(author);
        }

        // GET: Authors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                .FirstOrDefaultAsync(m => m.AuthorId == id);
            if (author == null)
            {
                return NotFound();
            }

            return View(author);
        }

        // POST: Authors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author != null)
            {
                _context.Authors.Remove(author);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AuthorExists(int id)
        {
            return _context.Authors.Any(e => e.AuthorId == id);
        }
    }
}
