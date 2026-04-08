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
    public class ReadersController : Controller
    {
        private readonly LibraryDbContext _context;

        public ReadersController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Readers
        public async Task<IActionResult> Index()
        {
            var readers = await _context.Readers
                .Include(r => r.Department)
                    .ThenInclude(d => d.Faculty)
                .Include(r => r.Position)
                .ToListAsync();

            var registrationStats = readers
                .Where(r => r.RegistrationDate.HasValue)
                .GroupBy(r => r.RegistrationDate!.Value.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ToList();

            ViewBag.RegistrationYearLabels = registrationStats.Select(x => x.Year).ToList();
            ViewBag.RegistrationYearCounts = registrationStats.Select(x => x.Count).ToList();

            return View(readers);
        }
        public async Task<IActionResult> ExportToExcel()
        {
            var readers = await _context.Readers
                .Include(r => r.Department)
                    .ThenInclude(d => d.Faculty)
                .Include(r => r.Position)
                .OrderBy(r => r.FullName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Readers");

            worksheet.Cell(1, 1).Value = "ПІБ";
            worksheet.Cell(1, 2).Value = "Факультет";
            worksheet.Cell(1, 3).Value = "Кафедра";
            worksheet.Cell(1, 4).Value = "Посада";
            worksheet.Cell(1, 5).Value = "Дата реєстрації";

            for (int i = 0; i < readers.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = readers[i].FullName;
                worksheet.Cell(i + 2, 2).Value = readers[i].Department?.Faculty?.FacultyName;
                worksheet.Cell(i + 2, 3).Value = readers[i].Department?.DepartmentName;
                worksheet.Cell(i + 2, 4).Value = readers[i].Position?.PositionName;
                worksheet.Cell(i + 2, 5).Value = readers[i].RegistrationDate?.ToString("yyyy-MM-dd");
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Readers.xlsx");
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
                var facultyName = row.Cell(2).GetString().Trim();
                var departmentName = row.Cell(3).GetString().Trim();
                var positionName = row.Cell(4).GetString().Trim();

                if (string.IsNullOrWhiteSpace(fullName) ||
                    string.IsNullOrWhiteSpace(facultyName) ||
                    string.IsNullOrWhiteSpace(departmentName) ||
                    string.IsNullOrWhiteSpace(positionName))
                    continue;

                var faculty = await _context.Faculties
                    .FirstOrDefaultAsync(f => f.FacultyName == facultyName);

                if (faculty == null)
                    continue;

                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentName == departmentName && d.FacultyId == faculty.FacultyId);

                if (department == null)
                    continue;

                var position = await _context.Positions
                    .FirstOrDefaultAsync(p => p.PositionName == positionName);

                if (position == null)
                    continue;

                DateOnly registrationDate;
                var dateCell = row.Cell(5);

                if (dateCell.DataType == XLDataType.DateTime)
                {
                    registrationDate = DateOnly.FromDateTime(dateCell.GetDateTime());
                }
                else
                {
                    var registrationDateText = dateCell.GetString().Trim();

                    if (!DateOnly.TryParseExact(registrationDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out registrationDate))
                        continue;
                }

                var existingReader = await _context.Readers
                    .FirstOrDefaultAsync(r =>
                        r.FullName == fullName &&
                        r.DepartmentId == department.DepartmentId &&
                        r.RegistrationDate == registrationDate);

                if (existingReader == null)
                {
                    _context.Readers.Add(new Reader
                    {
                        FullName = fullName,
                        FacultyId = faculty.FacultyId,
                        DepartmentId = department.DepartmentId,
                        PositionId = position.PositionId,
                        RegistrationDate = registrationDate
                    });
                }
                else
                {
                    existingReader.FacultyId = faculty.FacultyId;
                    existingReader.DepartmentId = department.DepartmentId;
                    existingReader.PositionId = position.PositionId;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Імпорт Excel виконано успішно.";
            return RedirectToAction(nameof(Index));
        }
        // GET: Readers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reader = await _context.Readers
    .Include(r => r.Department)
        .ThenInclude(d => d.Faculty)
    .Include(r => r.Position)
    .Include(r => r.Borrowings)
        .ThenInclude(b => b.Copy)
    .FirstOrDefaultAsync(m => m.ReaderId == id);

            if (reader == null)
            {
                return NotFound();
            }

            return View(reader);
        }

        // GET: Readers/Create
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName");
            ViewData["PositionId"] = new SelectList(_context.Positions, "PositionId", "PositionName");
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName");
            return View();
        }

        // POST: Readers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReaderId,FullName,DepartmentId,PositionId,RegistrationDate")] Reader reader)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reader);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", reader.DepartmentId);
            ViewData["PositionId"] = new SelectList(_context.Positions, "PositionId", "PositionName", reader.PositionId);
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", reader.FacultyId);
            return View(reader);
        }

        // GET: Readers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reader = await _context.Readers.FindAsync(id);
            if (reader == null)
            {
                return NotFound();
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", reader.DepartmentId);
            ViewData["PositionId"] = new SelectList(_context.Positions, "PositionId", "PositionName", reader.PositionId);
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", reader.FacultyId);
            return View(reader);
        }

        // POST: Readers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReaderId,FullName,DepartmentId,PositionId,RegistrationDate")] Reader reader)
        {
            if (id != reader.ReaderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reader);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReaderExists(reader.ReaderId))
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

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", reader.DepartmentId);
            ViewData["PositionId"] = new SelectList(_context.Positions, "PositionId", "PositionName", reader.PositionId);
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", reader.FacultyId);
            return View(reader);
        }

        // GET: Readers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reader = await _context.Readers
    .Include(r => r.Department)
        .ThenInclude(d => d.Faculty)
    .Include(r => r.Position)
    .FirstOrDefaultAsync(m => m.ReaderId == id);
            if (reader == null)
            {
                return NotFound();
            }

            return View(reader);
        }

        // POST: Readers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reader = await _context.Readers.FindAsync(id);
            if (reader != null)
            {
                _context.Readers.Remove(reader);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReaderExists(int id)
        {
            return _context.Readers.Any(e => e.ReaderId == id);
        }
    }
}
