using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HR.Data;
using HR.Models;
using Microsoft.AspNetCore.Authorization;
using NToastNotify;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class FamilieController : Controller
    {
        private static int angajatId;
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        public FamilieController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: Families
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Familie.Include(f => f.Angajat);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Families/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var familie = await _context.Familie
                .Include(f => f.Angajat)
                .FirstOrDefaultAsync(m => m.AngajatId == id);
            if (familie == null)
            {
                return NotFound();
            }

            return View(familie);
        }

        // GET: Families/Create
        public IActionResult Create(int id)
        {
            angajatId = id;
            ViewData["AngajatId"] = new SelectList(_context.Angajat, "Id", "Id");
            return View();
        }

        // POST: Families/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AngajatId,NumeSot,DataNasteriiSot,LocNasteriiSot,ProfesieSot,LocDeMuncaSot,PrenumeCopil1,DataNasteriiCopil1,PrenumeCopil2,DataNasteriiCopil2,PrenumeCopil3,DataNasteriiCopil3,PrenumeCopil4,DataNasteriiCopil4")] Familie familie)
        {
            familie.AngajatId = angajatId;
            if (ModelState.IsValid)
            {
                familie.Id = 0;
                _context.Add(familie);
                await _context.SaveChangesAsync();
                var evolutiePersonala = await _context.EvolutiePersonala.FirstOrDefaultAsync(e => e.AngajatId == angajatId);
                if(evolutiePersonala == null)
                {
                    return RedirectToAction("Create", "EvolutiePersonala", new { @id = angajatId });
                }
                else
                {
                    return RedirectToAction("Edit", "EvolutiePersonala", new { @id = angajatId });
                }
            }
            // ViewData["AngajatId"] = new SelectList(_context.Angajat, "Id", "Id", familie.AngajatId);
            return View(familie);
        }

        // GET: Families/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var familie = await _context.Familie.FirstOrDefaultAsync(f => f.AngajatId == id);
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == id);
            var nume = angajat.Nume + " " + angajat.Prenume;
            ViewBag.Nume = nume;
            if (familie == null)
            {
                return RedirectToAction("Create", new { id = id });
            }
            return View(familie);
        }

        // POST: Families/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AngajatId,NumeSot,DataNasteriiSot,LocNasteriiSot,ProfesieSot,LocDeMuncaSot,PrenumeCopil1,DataNasteriiCopil1,PrenumeCopil2,DataNasteriiCopil2,PrenumeCopil3,DataNasteriiCopil3,PrenumeCopil4,DataNasteriiCopil4")] Familie familie)
        {
            if (id != familie.Id)
            {
                return NotFound();
            }
            var familieDb = await _context.Familie.FirstOrDefaultAsync(f => f.AngajatId == id);
            var local = _context.Set<Familie>()
           .Local
           .FirstOrDefault(entry => entry.Id.Equals(familieDb.Id));
            _context.Entry(local).State = EntityState.Detached;
            _context.Entry(familieDb).State = EntityState.Detached;
            familie.Id = familieDb.Id;
            familie.AngajatId = id;
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(familie);
                    await _context.SaveChangesAsync();
                    _toastNotification.AddSuccessToastMessage("Datele au fost modificate cu success!");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FamilieExists(familie.Id))
                    {
                        _toastNotification.AddErrorToastMessage("Ceva nu a mers bine!");
                        return NotFound();
                    }
                    else
                    {
                        _toastNotification.AddErrorToastMessage("Ceva nu a mers bine!");
                        throw;
                    }
                }

            }
            return View(familie);
        }

        // GET: Families/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var familie = await _context.Familie
                .Include(f => f.Angajat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (familie == null)
            {
                return NotFound();
            }

            return View(familie);
        }

        // POST: Families/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var familie = await _context.Familie.FindAsync(id);
            _context.Familie.Remove(familie);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FamilieExists(int id)
        {
            return _context.Familie.Any(e => e.Id == id);
        }
    }
}
