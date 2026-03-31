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
            return View(await _context.PublicationTypes.ToListAsync());
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
