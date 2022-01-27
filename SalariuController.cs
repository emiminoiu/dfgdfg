using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR.Data;
using HR.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System;
using NToastNotify;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class SalariuController : Controller
    {
        #region privateFields
        private static int angajatId;
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        #endregion
        #region ctor
        public SalariuController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }
        #endregion

        #region publicMethods
        // GET: Salarius
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Salariu.Include(s => s.Angajat);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Salarius/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salariu = await _context.Salariu
                .Include(s => s.Angajat)
                .FirstOrDefaultAsync(m => m.AngajatId == id);
            if (salariu == null)
            {
                return NotFound();
            }

            return View(salariu);
        }

        // GET: Salarius/Create
        public async Task<IActionResult> Create(int Id)
        {
            angajatId = Id;
            var evolutiePersonala = await _context.EvolutiePersonala.FirstOrDefaultAsync(ev => ev.AngajatId == angajatId);
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == angajatId);
            var salariu = new Salariu();
            if (evolutiePersonala != null)
            {
                var vechime = evolutiePersonala.VechimeTotalaInvatamant;
                if (!string.IsNullOrEmpty(angajat.FunctieDidactica))
                {
                    salariu.SalariuNet = await CalculareSalariuNetPentruFunctiiDidactice(angajat, vechime);
                }
                if (!string.IsNullOrEmpty(angajat.Functia))
                {
                    salariu.SalariuNet = await CalculareSalariuNetPentruFunctiiConducere(angajat, angajat.GradulFunctieiDeConducere);
                }
                await _context.SaveChangesAsync();
            }
           
            return View(salariu);
        }

        // POST: Salarius/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AngajatId,SalariuLG_Real,SalariuLG_Taxa,SalariuMinimEconomie,SalariuBaza,SalariuPeAnumiteCateg,SalariuHG,SalariuHG_Real,SalariuHG_Taxa,SalariuMinimGrilaVeche,SalariuMaximGrilaVeche,SalariuBazaNou,SalariuDec2018Taxa,SalariuDec2018Buget,SalariuNet,SalariuBrut")] Salariu salariu)
        {
            salariu.AngajatId = angajatId;
            if (ModelState.IsValid)
            {
                salariu.Id = 0;
                _context.Add(salariu);
                await _context.SaveChangesAsync();
                var sporuri = await _context.Sporuri.FirstOrDefaultAsync(e => e.AngajatId == angajatId);
                if (sporuri == null)
                {
                    return RedirectToAction("Create", "Sporuri", new { @id = angajatId });
                }
                else
                {
                    return RedirectToAction("Edit", "Sporuri", new { @id = angajatId });
                }
               
            }
            return View(salariu);
        }

        // GET: Salarius/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var evolutiePersonala = await _context.EvolutiePersonala.FirstOrDefaultAsync(ev => ev.AngajatId == id);
            var salariu = await _context.Salariu.FirstOrDefaultAsync(s => s.AngajatId == id);
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == id);
            var nume = angajat.Nume + " " + angajat.Prenume;
            if (evolutiePersonala != null)
            {
                var vechime = evolutiePersonala.VechimeTotalaInvatamant;
                if (!string.IsNullOrEmpty(angajat.FunctieDidactica))
                {
                    salariu.SalariuNet = await CalculareSalariuNetPentruFunctiiDidactice(angajat, vechime);
                }
                if (!string.IsNullOrEmpty(angajat.Functia))
                {
                    salariu.SalariuNet = await CalculareSalariuNetPentruFunctiiConducere(angajat, angajat.GradulFunctieiDeConducere);
                }
                await _context.SaveChangesAsync();
            }

            ViewBag.Nume = nume;
            if (salariu == null)
            {
                return RedirectToAction("Create", new { id = id });
            }
            return View(salariu);
        }

        // POST: Salarius/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AngajatId,SalariuLG_Real,SalariuLG_Taxa,SalariuMinimEconomie,SalariuBaza,SalariuPeAnumiteCateg,SalariuHG,SalariuHG_Real,SalariuHG_Taxa,SalariuMinimGrilaVeche,SalariuMaximGrilaVeche,SalariuBazaNou,SalariuDec2018Taxa,SalariuDec2018Buget,SalariuNet,SalariuBrut")] Salariu salariu)
        {
            if (id != salariu.Id)
            {
                return NotFound();
            }

            var salariuDb = await _context.Salariu.FirstOrDefaultAsync(f => f.AngajatId == id);
            var local = _context.Set<Salariu>()
           .Local
           .FirstOrDefault(entry => entry.Id.Equals(salariuDb.Id));
            _context.Entry(local).State = EntityState.Detached;
            _context.Entry(salariuDb).State = EntityState.Detached;
            salariu.AngajatId = id;
            salariu.Id = salariuDb.Id;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(salariu);
                    await _context.SaveChangesAsync();
                    _toastNotification.AddSuccessToastMessage("Datele au fost modificate cu success!");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SalariuExists(salariu.Id))
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
            return View(salariu);
        }

        // GET: Salarius/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salariu = await _context.Salariu
                .Include(s => s.Angajat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (salariu == null)
            {
                return NotFound();
            }

            return View(salariu);
        }

        // POST: Salarius/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var salariu = await _context.Salariu.FindAsync(id);
            _context.Salariu.Remove(salariu);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SalariuExists(int id)
        {
            return _context.Salariu.Any(e => e.Id == id);
        }
        #endregion
        #region privateMethods
        private async Task<decimal> CalculareSalariuNetPentruFunctiiDidactice(Angajat angajat, int vechimea)
        {
            var functiiDidacticePosibile = await _context.GradeDidactice.Where(g => g.Nume == angajat.FunctieDidactica).ToListAsync();
            decimal salariuNet = 0;
            foreach (var functie in functiiDidacticePosibile)
            {
                string[] interval = functie.IntervalAni.Split('-');
                var startIntervalAni = Int32.Parse(interval[0]);
                var finalIntervalAni = Int32.Parse(interval[1]);
                if (vechimea >= startIntervalAni && vechimea <= finalIntervalAni)
                {
                    salariuNet = functie.SalariuBaza;
                    angajat.GradDidacticId = functie.Id;
                }
            }
            return salariuNet;
        }
        
        private async Task<decimal> CalculareSalariuNetPentruFunctiiConducere(Angajat angajat, int? gradul)
        {
            var functiiConducere = await _context.FunctiiConducere.FirstOrDefaultAsync(g => g.Nume == angajat.Functia && g.Grad == gradul);
            angajat.FunctieConducereId = functiiConducere.Id;
            decimal salariuNet = functiiConducere.SalariuBaza;
            return salariuNet;
        }
        #endregion
    }
}
