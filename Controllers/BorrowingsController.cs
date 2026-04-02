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
