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
            var libraryDbContext = _context.Copies
                .Include(c => c.Publication);

            return View(await libraryDbContext.ToListAsync());
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
