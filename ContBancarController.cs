using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR.Data;
using HR.Models;
using NToastNotify;
using Microsoft.AspNetCore.Authorization;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class ContBancarController : Controller
    {
        private static int AngajatId;
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;

        public ContBancarController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: ContBancars
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ContBancar.Include(c => c.Angajat);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ContBancars/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contBancar = await _context.ContBancar
                .Include(c => c.Angajat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contBancar == null)
            {
                return NotFound();
            }

            return View(contBancar);
        }

        // GET: ContBancars/Create
        public IActionResult Create(int Id)
        {
            AngajatId = Id;
            return View();
        }

        // POST: ContBancars/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AngajatId,OrdinPlata,NrOrdine,Calc,SP,NrOrd,NrOrdC,NrOrdP,Social,Social1")] ContBancar contBancar)
        {
            contBancar.AngajatId = AngajatId;
            if (ModelState.IsValid)
            {
                contBancar.Id = 0;
                _context.Add(contBancar);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Angajat");
            }
            return View(contBancar);
        }

        // GET: ContBancars/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contBancar = await _context.ContBancar.FirstOrDefaultAsync(f => f.AngajatId == id);
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == id);
            var nume = angajat.Nume + " " + angajat.Prenume;
            ViewBag.Nume = nume;
            if (contBancar == null)
            {
                return RedirectToAction("Create", new { id = id });
            }
            return View(contBancar);
        }

        // POST: ContBancars/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AngajatId,OrdinPlata,NrOrdine,Calc,SP,NrOrd,NrOrdC,NrOrdP,Social,Social1")] ContBancar contBancar)
        {
            if (id != contBancar.Id)
            {
                return NotFound();
            }
          
            var contBancarDb = await _context.ContBancar.FirstOrDefaultAsync(f => f.AngajatId == id);
            var local = _context.Set<ContBancar>()
           .Local
           .FirstOrDefault(entry => entry.Id.Equals(contBancarDb.Id));
            _context.Entry(local).State = EntityState.Detached;
            _context.Entry(contBancarDb).State = EntityState.Detached;
            contBancar.AngajatId = id;
            contBancar.Id = contBancarDb.Id;
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contBancar);
                    await _context.SaveChangesAsync();
                    _toastNotification.AddSuccessToastMessage("Datele au fost modificate cu success!");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContBancarExists(contBancar.Id))
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
            return View(contBancar);
        }

        // GET: ContBancars/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contBancar = await _context.ContBancar
                .Include(c => c.Angajat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contBancar == null)
            {
                return NotFound();
            }

            return View(contBancar);
        }

        // POST: ContBancars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contBancar = await _context.ContBancar.FindAsync(id);
            _context.ContBancar.Remove(contBancar);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContBancarExists(int id)
        {
            return _context.ContBancar.Any(e => e.Id == id);
        }
    }
}
