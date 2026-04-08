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
    public class DepartmentsController : Controller
    {
        private readonly LibraryDbContext _context;

        public DepartmentsController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Departments
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .Include(d => d.Faculty)
                .ToListAsync();

            var departmentStats = await _context.Departments
                .Select(d => new
                {
                    DepartmentName = string.IsNullOrWhiteSpace(d.DepartmentName) ? "Невідомо" : d.DepartmentName.Trim(),
                    Count = _context.Readers.Count(r => r.DepartmentId == d.DepartmentId)
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.DepartmentName)
                .ToListAsync();

            ViewBag.DepartmentLabels = departmentStats.Select(x => x.DepartmentName).ToList();
            ViewBag.DepartmentCounts = departmentStats.Select(x => x.Count).ToList();

            return View(departments);
        }
        public async Task<IActionResult> ExportToExcel()
        {
            var departments = await _context.Departments
                .Include(d => d.Faculty)
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Departments");

            worksheet.Cell(1, 1).Value = "Назва кафедри";
            worksheet.Cell(1, 2).Value = "Факультет";

            for (int i = 0; i < departments.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = departments[i].DepartmentName;
                worksheet.Cell(i + 2, 2).Value = departments[i].Faculty?.FacultyName;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Departments.xlsx");
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
                var departmentName = row.Cell(1).GetString().Trim();
                var facultyName = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(departmentName) || string.IsNullOrWhiteSpace(facultyName))
                    continue;

                var faculty = await _context.Faculties
                    .FirstOrDefaultAsync(f => f.FacultyName == facultyName);

                if (faculty == null)
                    continue;

                var existingDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentName == departmentName && d.FacultyId == faculty.FacultyId);

                if (existingDepartment == null)
                {
                    _context.Departments.Add(new Department
                    {
                        DepartmentName = departmentName,
                        FacultyId = faculty.FacultyId
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Імпорт Excel виконано успішно.";
            return RedirectToAction(nameof(Index));
        }


        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(d => d.Faculty)
                .Include(d => d.Readers)
                .FirstOrDefaultAsync(m => m.DepartmentId == id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // GET: Departments/Create
        public IActionResult Create()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName");
            return View();
        }

        // POST: Departments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepartmentId,DepartmentName,FacultyId")] Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", department.FacultyId);
            return View(department);
        }

        // GET: Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", department.FacultyId);
            return View(department);
        }

        // POST: Departments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DepartmentId,DepartmentName,FacultyId")] Department department)
        {
            if (id != department.DepartmentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DepartmentId))
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
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", department.FacultyId);
            return View(department);
        }

        // GET: Departments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(d => d.Faculty)
                .FirstOrDefaultAsync(m => m.DepartmentId == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.DepartmentId == id);
        }
    }
}
