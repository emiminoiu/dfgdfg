using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR.Data;
using HR.Models;
using Microsoft.AspNetCore.Authorization;
using NToastNotify;
using Microsoft.Extensions.Configuration;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class FunctiiController : Controller
    {
        private static int AngajatId;
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
    
        public FunctiiController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
         
        }

        // GET: Functiis
        public async Task<IActionResult> Index()
        {

            var applicationDbContext = _context.Functii.OrderByDescending(f => f.Id).Include(f => f.Angajat);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Functiis/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var functii = await _context.Functii
                .Include(f => f.Angajat)
                .FirstOrDefaultAsync(m => m.AngajatId == id);
            if (functii == null)
            {
                return NotFound();
            }

            return View(functii);
        }

        // GET: Functiis/Create
        public IActionResult Create(int Id)
        {
            AngajatId = Id;
            return View();
        }

        // POST: Functiis/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AngajatId,CodNom,CodNom_N,DenNom,DenFunctie,TR_GR_TR,Functia,Anexa,AST,SalariuMinim,SalariuMaxim,SalriuRef,SalariuMinim160,SalariuMinim161,SalariuMinim162,SalariuMinim163,SalariuMinim164,SalariuMinim165,SalariuLG153,PAC")] Functii functii)
        {
            functii.AngajatId = AngajatId;

            if (ModelState.IsValid)
            {
                functii.Id = 0;
                _context.Add(functii);
                await _context.SaveChangesAsync();
                var contBancar = await _context.ContBancar.FirstOrDefaultAsync(e => e.AngajatId == AngajatId);
                if (contBancar == null)
                {
                    return RedirectToAction("Create", "ContBancar", new { @id = AngajatId });
                }
                else
                {
                    return RedirectToAction("Edit", "ContBancar", new { @id = AngajatId });
                }
                
            }
            return View(functii);
        }

        // GET: Functiis/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var functii = await _context.Functii.FirstOrDefaultAsync(f => f.AngajatId == id);
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == id);
            var nume = angajat.Nume + " " + angajat.Prenume;
            ViewBag.Nume = nume;
            if (functii == null)
            {
                return RedirectToAction("Create", new { id = id });
            }
            return View(functii);
        }

        // POST: Functiis/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AngajatId,CodNom,CodNom_N,DenNom,DenFunctie,TR_GR_TR,Functia,Anexa,AST,SalariuMinim,SalariuMaxim,SalriuRef,SalariuMinim160,SalariuMinim161,SalariuMinim162,SalariuMinim163,SalariuMinim164,SalariuMinim165,SalariuLG153,PAC")] Functii functii)
        {
            if (id != functii.Id)
            {
                return NotFound();
            }
            var functiiDb = await _context.Functii.FirstOrDefaultAsync(f => f.AngajatId == id);
            var local = _context.Set<Functii>()
           .Local
           .FirstOrDefault(entry => entry.Id.Equals(functiiDb.Id));
            _context.Entry(local).State = EntityState.Detached;
            _context.Entry(functiiDb).State = EntityState.Detached;
            functii.AngajatId = id;
            functii.Id = functiiDb.Id;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(functii);
                    await _context.SaveChangesAsync();
                    _toastNotification.AddSuccessToastMessage("Datele au fost modificate cu success!");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FunctiiExists(functii.Id))
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
            return View(functii);
        }

        // GET: Functiis/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var functii = await _context.Functii
                .Include(f => f.Angajat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (functii == null)
            {
                return NotFound();
            }

            return View(functii);
        }

        // POST: Functiis/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var functii = await _context.Functii.FindAsync(id);
            _context.Functii.Remove(functii);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FunctiiExists(int id)
        {
            return _context.Functii.Any(e => e.Id == id);
        }
    }
}
