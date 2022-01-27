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
    public class EvolutiePersonalaController : Controller
    {
        private static int angajatId;
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        public EvolutiePersonalaController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: EvolutiePersonalas
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.EvolutiePersonala.Include(e => e.Angajat);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: EvolutiePersonalas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evolutiePersonala = await _context.EvolutiePersonala
                .Include(e => e.Angajat)
                .FirstOrDefaultAsync(m => m.AngajatId == id);
            if (evolutiePersonala == null)
            {
                return NotFound();
            }

            return View(evolutiePersonala);
        }

        // GET: EvolutiePersonalas/Create
        public IActionResult Create(int id)
        {
            angajatId = id;
            return View();
        }

        // POST: EvolutiePersonalas/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AngajatId,UScoalaAbsolvita,DataAbsolviriiScolii,LocalitateaUScoliAbsolvite,UniversitateDR1,LocalitateDR1,TitluStintificDR1,DataDR1,TaraDr1,UniversitateDR2,LocalitateDR2,TitluStintificDR2,DataDR2,TaraDr2,VechimeTotalaAngajare,VechimeTotalaInvatamant")] EvolutiePersonala evolutiePersonala)
        {
            evolutiePersonala.AngajatId = angajatId;
            if (ModelState.IsValid)
            {
                 evolutiePersonala.Id = 0;
                _context.Add(evolutiePersonala);
                await _context.SaveChangesAsync();
                var evolutieLocDeMunca = await _context.EvolutieLocDeMunca.FirstOrDefaultAsync(e => e.AngajatId == angajatId);
                if (evolutieLocDeMunca == null)
                {
                    return RedirectToAction("Create", "EvolutieLocDeMunca", new { @id = angajatId });
                }
                else
                {
                    return RedirectToAction("Edit", "EvolutieLocDeMunca", new { @id = angajatId });
                }
               
            }
            //ViewData["AngajatId"] = new SelectList(_context.Angajat, "Id", "Id", evolutiePersonala.AngajatId);
            return View(evolutiePersonala);
        }

        // GET: EvolutiePersonalas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }  
            var evolutiePersonala = await _context.EvolutiePersonala.FirstOrDefaultAsync(e => e.AngajatId == id);
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == id);
            var nume = angajat.Nume + " " + angajat.Prenume;
            ViewBag.Nume = nume;
            if (evolutiePersonala == null)
            {
                return RedirectToAction("Create", new { id = id });
            }
            return View(evolutiePersonala);
        }

        // POST: EvolutiePersonalas/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AngajatId,UScoalaAbsolvita,DataAbsolviriiScolii,LocalitateaUScoliAbsolvite,UniversitateDR1,LocalitateDR1,TitluStintificDR1,DataDR1,TaraDr1,UniversitateDR2,LocalitateDR2,TitluStintificDR2,DataDR2,TaraDr2,LimbiStraine,VechimeTotalaAngajare,VechimeTotalaInvatamant")] EvolutiePersonala evolutiePersonala)
        {
            if (id != evolutiePersonala.Id)
            {
                return NotFound();
            }
            var evolutiePersonalaDb = await _context.EvolutiePersonala.FirstOrDefaultAsync(ep => ep.AngajatId == id);
            var local = _context.Set<EvolutiePersonala>()
           .Local
           .FirstOrDefault(entry => entry.Id.Equals(evolutiePersonalaDb.Id));
            _context.Entry(local).State = EntityState.Detached;
            _context.Entry(evolutiePersonalaDb).State = EntityState.Detached;
            evolutiePersonala.AngajatId = id;
            evolutiePersonala.Id = evolutiePersonalaDb.Id;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(evolutiePersonala);
                    await _context.SaveChangesAsync();
                    _toastNotification.AddSuccessToastMessage("Datele au fost modificate cu success!");

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EvolutiePersonalaExists(evolutiePersonala.Id))
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
            return View(evolutiePersonala);
        }

        // GET: EvolutiePersonalas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evolutiePersonala = await _context.EvolutiePersonala
                .Include(e => e.Angajat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (evolutiePersonala == null)
            {
                return NotFound();
            }

            return View(evolutiePersonala);
        }

        // POST: EvolutiePersonalas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evolutiePersonala = await _context.EvolutiePersonala.FindAsync(id);
            _context.EvolutiePersonala.Remove(evolutiePersonala);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EvolutiePersonalaExists(int id)
        {
            return _context.EvolutiePersonala.Any(e => e.Id == id);
        }
    }
}
