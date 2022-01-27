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
    public class EvolutieLocDeMuncaController : Controller
    {
        private static int angajatId;
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        public EvolutieLocDeMuncaController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: EvolutieLocDeMuncas
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.EvolutieLocDeMunca.Include(e => e.Angajat);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: EvolutieLocDeMuncas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evolutieLocDeMunca = await _context.EvolutieLocDeMunca
                .Include(e => e.Angajat)
                .FirstOrDefaultAsync(m => m.AngajatId == id);
            if (evolutieLocDeMunca == null)
            {
                return NotFound();
            }

            return View(evolutieLocDeMunca);
        }

        // GET: EvolutieLocDeMuncas/Create
        public IActionResult Create(int Id)
        {
            angajatId = Id;
            List<string> tipContractDeMunca = new List<string>();
            tipContractDeMunca.Add("Contract pe perioada determinata");
            tipContractDeMunca.Add("Contract pe perioada nedeterminata");
            ViewData["tipContract"] = tipContractDeMunca;
            var year = DateTime.Now.Year;
            ViewBag.AnMinus5 = $"{year - 5}";
            ViewBag.AnMinus4 = $"{year - 4}";
            ViewBag.AnMinus3 = $"{year - 3}";
            ViewBag.AnMinus2 = $"{year - 2}";
            ViewBag.AnMinus1 = $"{year - 1}";
            return View();
        }

        // POST: EvolutieLocDeMuncas/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AngajatId,DataStartPreparatorD,DataFinalPreparatorD,DataStartPreparatorT" +
            ",DataFinalPreparatorT,DataStartAsistentD,DataFinalAsistentD,DataStartAsistentT,DataFinalAsistentT,DataStartLectorD" +
            ",DataFinalLectorD,DataStartLectorT,DataFinalLectorT,DataStartConfD,DataFinalConfD,DataStartConfT,DataFinalConfT" +
            ",DataStartProfD,DataFinalProfD,DataStartProfT,DataFinalProfT" +
            ",AnMinus5,AnMinus4,AnMinus3,AnMinus2,AnMinus1,DataFinalPerioadaDeterminata" +
            ",DataStartPerioadaDeterminata,NrContract,TipContract,LocatiaAngajarii,DataAngajariiFctAuxiliara" +
            ",DataAngajariiInvatamant,DataAngajariiFctDidactica,SerieCarnetMunca")] EvolutieLocDeMunca evolutieLocDeMunca)
        {

            evolutieLocDeMunca.AngajatId = angajatId;
            if (ModelState.IsValid)
            {
                 evolutieLocDeMunca.Id = 0;
                _context.Add(evolutieLocDeMunca);
                await _context.SaveChangesAsync();
                var salariu = await _context.Salariu.FirstOrDefaultAsync(e => e.AngajatId == angajatId);
                if (salariu == null)
                {
                    return RedirectToAction("Create", "Salariu", new { @id = angajatId });
                }
                else
                {
                    return RedirectToAction("Edit", "Salariu", new { @id = angajatId });
                }
                
            }
            List<string> tipContractDeMunca = new List<string>();
            tipContractDeMunca.Add("Contract pe perioada determinata");
            tipContractDeMunca.Add("Contract pe perioada nedeterminata");
            ViewData["tipContract"] = tipContractDeMunca;
            var year = DateTime.Now.Year;
            ViewBag.AnMinus5 = $"{year - 5}";
            ViewBag.AnMinus4 = $"{year - 4}";
            ViewBag.AnMinus3 = $"{year - 3}";
            ViewBag.AnMinus2 = $"{year - 2}";
            ViewBag.AnMinus1 = $"{year - 1}";
            return View(evolutieLocDeMunca);
        }

        // GET: EvolutieLocDeMuncas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evolutieLocDeMunca = await _context.EvolutieLocDeMunca.FirstOrDefaultAsync(e => e.AngajatId == id);
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == id);
            var nume = angajat.Nume + " " + angajat.Prenume;
            ViewBag.Nume = nume;
            var year = DateTime.Now.Year;
            ViewBag.AnMinus5 = $"{year - 5}";
            ViewBag.AnMinus4 = $"{year - 4}";
            ViewBag.AnMinus3 = $"{year - 3}";
            ViewBag.AnMinus2 = $"{year - 2}";
            ViewBag.AnMinus1 = $"{year - 1}";
            List<string> tipContractDeMunca = new List<string>();
            tipContractDeMunca.Add("Contract pe perioada determinata");
            tipContractDeMunca.Add("Contract pe perioada nedeterminata");
            ViewData["tipContract"] = tipContractDeMunca;
            if (evolutieLocDeMunca == null)
            {
                return RedirectToAction("Create", new { id = id });
            }
            return View(evolutieLocDeMunca);
        }

        // POST: EvolutieLocDeMuncas/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AngajatId,DataStartPreparatorD,DataFinalPreparatorD,DataStartPreparatorT," +
            "DataFinalPreparatorT,DataStartAsistentD,DataFinalAsistentD,DataStartAsistentT,DataFinalAsistentT,DataStartLectorD,DataFinalLectorD," +
            "DataStartLectorT,DataFinalLectorT,DataStartConfD,DataFinalConfD,DataStartConfT,DataFinalConfT,DataStartProfD,DataFinalProfD," +
            "DataStartProfT,DataFinalProfT,AnMinus5,AnMinus4,AnMinus3,AnMinus2,AnMinus1,DataFinalPerioadaDeterminata" +
            ",DataStartPerioadaDeterminata,NrContract,TipContract,LocatiaAngajarii,DataAngajariiFctAuxiliara" +
            ",DataAngajariiInvatamant,DataAngajariiFctDidactica,SerieCarnetMunca")] EvolutieLocDeMunca evolutieLocDeMunca)
        {
            if (id != evolutieLocDeMunca.Id)
            {
                return NotFound();
            }
            var evolutieMuncaDb = await _context.EvolutieLocDeMunca.FirstOrDefaultAsync(f => f.AngajatId == id);
            var local = _context.Set<EvolutieLocDeMunca>()
           .Local
           .FirstOrDefault(entry => entry.Id.Equals(evolutieMuncaDb.Id));
            _context.Entry(local).State = EntityState.Detached;
            _context.Entry(evolutieMuncaDb).State = EntityState.Detached;
            evolutieLocDeMunca.AngajatId = id;
            evolutieLocDeMunca.Id = evolutieMuncaDb.Id;
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(evolutieLocDeMunca);
                    await _context.SaveChangesAsync();
                    _toastNotification.AddSuccessToastMessage("Datele au fost modificate cu success!");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EvolutieLocDeMuncaExists(evolutieLocDeMunca.Id))
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
            List<string> tipContractDeMunca = new List<string>();
            tipContractDeMunca.Add("Contract pe perioada determinata");
            tipContractDeMunca.Add("Contract pe perioada nedeterminata");
            ViewData["tipContract"] = tipContractDeMunca;
            var year = DateTime.Now.Year;
            ViewBag.AnMinus5 = $"{year - 5}";
            ViewBag.AnMinus4 = $"{year - 4}";
            ViewBag.AnMinus3 = $"{year - 3}";
            ViewBag.AnMinus2 = $"{year - 2}";
            ViewBag.AnMinus1 = $"{year - 1}";
            return View(evolutieLocDeMunca);
        }

        // GET: EvolutieLocDeMuncas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evolutieLocDeMunca = await _context.EvolutieLocDeMunca
                .Include(e => e.Angajat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (evolutieLocDeMunca == null)
            {
                return NotFound();
            }

            return View(evolutieLocDeMunca);
        }

        // POST: EvolutieLocDeMuncas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evolutieLocDeMunca = await _context.EvolutieLocDeMunca.FindAsync(id);
            _context.EvolutieLocDeMunca.Remove(evolutieLocDeMunca);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EvolutieLocDeMuncaExists(int id)
        {
            return _context.EvolutieLocDeMunca.Any(e => e.Id == id);
        }
    }
}
