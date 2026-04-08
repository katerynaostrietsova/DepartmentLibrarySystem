using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryWebMvc.Models;

namespace LibraryWebMvc.Controllers
{
    public class FacultiesController : Controller
    {
        private readonly LibraryDbContext _context;

        public FacultiesController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Faculties
        public async Task<IActionResult> Index()
        {
            var faculties = await _context.Faculties.ToListAsync();

            var facultyStats = await _context.Faculties
                .Select(f => new
                {
                    FacultyName = string.IsNullOrWhiteSpace(f.FacultyName) ? "Невідомо" : f.FacultyName.Trim(),
                    Count = _context.Departments.Count(d => d.FacultyId == f.FacultyId)
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.FacultyName)
                .ToListAsync();

            ViewBag.FacultyLabels = facultyStats.Select(x => x.FacultyName).ToList();
            ViewBag.FacultyCounts = facultyStats.Select(x => x.Count).ToList();

            return View(faculties);
        }
        public async Task<IActionResult> ExportToExcel()
        {
            var faculties = await _context.Faculties
                .OrderBy(f => f.FacultyName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Faculties");

            worksheet.Cell(1, 1).Value = "Назва факультету";

            for (int i = 0; i < faculties.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = faculties[i].FacultyName;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Faculties.xlsx");
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
                var facultyName = row.Cell(1).GetString().Trim();

                if (string.IsNullOrWhiteSpace(facultyName))
                    continue;

                var existingFaculty = await _context.Faculties
                    .FirstOrDefaultAsync(f => f.FacultyName == facultyName);

                if (existingFaculty == null)
                {
                    _context.Faculties.Add(new Faculty
                    {
                        FacultyName = facultyName
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Імпорт Excel виконано успішно.";
            return RedirectToAction(nameof(Index));
        }
        // GET: Faculties/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faculty = await _context.Faculties
                .Include(f => f.Departments)
                .FirstOrDefaultAsync(m => m.FacultyId == id);

            if (faculty == null)
            {
                return NotFound();
            }

            return View(faculty);
        }

        // GET: Faculties/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Faculties/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FacultyId,FacultyName")] Faculty faculty)
        {
            if (ModelState.IsValid)
            {
                _context.Add(faculty);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(faculty);
        }

        // GET: Faculties/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null)
            {
                return NotFound();
            }
            return View(faculty);
        }

        // POST: Faculties/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FacultyId,FacultyName")] Faculty faculty)
        {
            if (id != faculty.FacultyId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(faculty);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FacultyExists(faculty.FacultyId))
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
            return View(faculty);
        }

        // GET: Faculties/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faculty = await _context.Faculties
                .FirstOrDefaultAsync(m => m.FacultyId == id);
            if (faculty == null)
            {
                return NotFound();
            }

            return View(faculty);
        }

        // POST: Faculties/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty != null)
            {
                _context.Faculties.Remove(faculty);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FacultyExists(int id)
        {
            return _context.Faculties.Any(e => e.FacultyId == id);
        }
    }
}
