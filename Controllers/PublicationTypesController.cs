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
    public class PublicationTypesController : Controller
    {
        private readonly LibraryDbContext _context;

        public PublicationTypesController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: PublicationTypes
        public async Task<IActionResult> Index()
        {
            var publicationTypes = await _context.PublicationTypes.ToListAsync();

            var typeStats = await _context.PublicationTypes
                .Select(pt => new
                {
                    TypeName = string.IsNullOrWhiteSpace(pt.TypeName) ? "Невідомо" : pt.TypeName.Trim(),
                    Count = _context.Publications.Count(p => p.PublicationTypeId == pt.PublicationTypeId)
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.TypeName)
                .ToListAsync();

            ViewBag.TypeLabels = typeStats.Select(x => x.TypeName).ToList();
            ViewBag.TypeCounts = typeStats.Select(x => x.Count).ToList();

            return View(publicationTypes);
        }

        public async Task<IActionResult> ExportToExcel()
        {
            var publicationTypes = await _context.PublicationTypes
                .OrderBy(pt => pt.TypeName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("PublicationTypes");

            worksheet.Cell(1, 1).Value = "Назва типу видання";

            for (int i = 0; i < publicationTypes.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = publicationTypes[i].TypeName;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PublicationTypes.xlsx");
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
                var typeName = row.Cell(1).GetString().Trim();

                if (string.IsNullOrWhiteSpace(typeName))
                    continue;

                var existingType = await _context.PublicationTypes
                    .FirstOrDefaultAsync(pt => pt.TypeName == typeName);

                if (existingType == null)
                {
                    _context.PublicationTypes.Add(new PublicationType
                    {
                        TypeName = typeName
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Імпорт Excel виконано успішно.";
            return RedirectToAction(nameof(Index));
        }
        // GET: PublicationTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publicationType = await _context.PublicationTypes
                .Include(pt => pt.Publications)
                .FirstOrDefaultAsync(m => m.PublicationTypeId == id);

            if (publicationType == null)
            {
                return NotFound();
            }

            return View(publicationType);
        }

        // GET: PublicationTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PublicationTypes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PublicationTypeId,TypeName")] PublicationType publicationType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(publicationType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(publicationType);
        }

        // GET: PublicationTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publicationType = await _context.PublicationTypes.FindAsync(id);
            if (publicationType == null)
            {
                return NotFound();
            }
            return View(publicationType);
        }

        // POST: PublicationTypes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PublicationTypeId,TypeName")] PublicationType publicationType)
        {
            if (id != publicationType.PublicationTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(publicationType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PublicationTypeExists(publicationType.PublicationTypeId))
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
            return View(publicationType);
        }

        // GET: PublicationTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publicationType = await _context.PublicationTypes
                .FirstOrDefaultAsync(m => m.PublicationTypeId == id);
            if (publicationType == null)
            {
                return NotFound();
            }

            return View(publicationType);
        }

        // POST: PublicationTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var publicationType = await _context.PublicationTypes.FindAsync(id);
            if (publicationType != null)
            {
                _context.PublicationTypes.Remove(publicationType);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PublicationTypeExists(int id)
        {
            return _context.PublicationTypes.Any(e => e.PublicationTypeId == id);
        }
    }
}
