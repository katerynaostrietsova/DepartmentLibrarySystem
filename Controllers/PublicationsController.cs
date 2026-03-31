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
            var libraryDbContext = _context.Publications
                .Include(p => p.Publisher)
                .Include(p => p.PublicationType);

            return View(await libraryDbContext.ToListAsync());
        }

        // GET: Publications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publication = await _context.Publications
                .Include(p => p.Copies)
                .Include(p => p.Publisher)
                .Include(p => p.PublicationType)
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
            ViewData["PublicationTypeId"] = new SelectList(_context.PublicationTypes, "PublicationTypeId", "PublicationTypeId", publication.PublicationTypeId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "PublisherId", "PublisherId", publication.PublisherId);
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
            ViewData["PublicationTypeId"] = new SelectList(_context.PublicationTypes, "PublicationTypeId", "PublicationTypeId", publication.PublicationTypeId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "PublisherId", "PublisherId", publication.PublisherId);
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
                .Include(p => p.PublicationType)
                .Include(p => p.Publisher)
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
