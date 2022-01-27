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
    public class SporuriController : Controller
    {
        private static int AngajatId;
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        public SporuriController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }
        // GET: Sporuris
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Sporuri.Include(s => s.Angajat);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Sporuris/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sporuri = await _context.Sporuri
                .Include(s => s.Angajat)
                .FirstOrDefaultAsync(m => m.AngajatId == id);
            if (sporuri == null)
            {
                return NotFound();
            }

            return View(sporuri);
        }

        // GET: Sporuris/Create
        public IActionResult Create(int Id)
        {
            AngajatId = Id;
            ViewData["AngajatId"] = new SelectList(_context.Angajat, "Id", "Id");
            return View();
        }

        // POST: Sporuris/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AngajatId,SporVechime,DataSporVechime,SporDoctor,SporStabilitate,ProcentIndConducere,SporStabilitateCM,SporDoctorVechi,SporDoctorTaxa,CuantumToxicitate,ProcentToxicitate,IndTranzitVeche,IndTranzitVecheTaxa,ProcentSporHandicVechi,CuantumSporHandicVechi,SporNoapte,SporExtra,IndTranzFeb2015,IndTranzFeb2015Taxa,IndTranz1,IndTranz2,IndTranzVeche")] Sporuri sporuri)
        {
            sporuri.AngajatId = AngajatId;
            if (ModelState.IsValid)
            {
                sporuri.Id = 0;
                _context.Add(sporuri);
                await _context.SaveChangesAsync();
                var functii = await _context.Functii.FirstOrDefaultAsync(e => e.AngajatId == AngajatId);
                if (functii == null)
                {
                    return RedirectToAction("Create", "Functii", new { @id = AngajatId });
                }
                else
                {
                    return RedirectToAction("Edit", "Functii", new { @id = AngajatId });
                }
              
            }
            return View(sporuri);
        }

        // GET: Sporuris/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sporuri = await _context.Sporuri.FirstOrDefaultAsync(s => s.AngajatId == id);
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == id);
            var nume = angajat.Nume + " " + angajat.Prenume;
            ViewBag.Nume = nume;
            if (sporuri == null)
            {
                return RedirectToAction("Create", new { id = id });
            }
            return View(sporuri);
        }

        // POST: Sporuris/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AngajatId,SporVechime,DataSporVechime,SporDoctor,SporStabilitate,ProcentIndConducere,SporStabilitateCM,SporDoctorVechi,SporDoctorTaxa,CuantumToxicitate,ProcentToxicitate,IndTranzitVeche,IndTranzitVecheTaxa,ProcentSporHandicVechi,CuantumSporHandicVechi,SporNoapte,SporExtra,IndTranzFeb2015,IndTranzFeb2015Taxa,IndTranz1,IndTranz2,IndTranzVeche")] Sporuri sporuri)
        {
            if (id != sporuri.Id)
            {
                return NotFound();
            }
            var sporuriDb = await _context.Sporuri.FirstOrDefaultAsync(f => f.AngajatId == id);
            var local = _context.Set<Sporuri>()
           .Local
           .FirstOrDefault(entry => entry.Id.Equals(sporuriDb.Id));
            _context.Entry(local).State = EntityState.Detached;
            _context.Entry(sporuriDb).State = EntityState.Detached;
            sporuri.AngajatId = id;
            sporuri.Id = sporuriDb.Id;
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sporuri);
                    await _context.SaveChangesAsync();
                    _toastNotification.AddSuccessToastMessage("Datele au fost modificate cu success!");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SporuriExists(sporuri.Id))
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
            return View(sporuri);
        }

        // GET: Sporuris/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sporuri = await _context.Sporuri
                .Include(s => s.Angajat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (sporuri == null)
            {
                return NotFound();
            }

            return View(sporuri);
        }

        // POST: Sporuris/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sporuri = await _context.Sporuri.FindAsync(id);
            _context.Sporuri.Remove(sporuri);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SporuriExists(int id)
        {
            return _context.Sporuri.Any(e => e.Id == id);
        }
    }
}
