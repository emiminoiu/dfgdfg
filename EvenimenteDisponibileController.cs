using HR.Data;
using HR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class EvenimenteDisponibile : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public EvenimenteDisponibile(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Sporuris
        public async Task<IActionResult> Index()
        {
            List<EvenimentDisponibil> myEvents = new List<EvenimentDisponibil>();
            var userId = _userManager.GetUserId(HttpContext.User);
            var userEvents = await _context.UtilizatorEvenimenteDisponibile.Where(up => up.UtilizatorId.Equals(userId)).ToListAsync();
            var allEvents = await _context.EvenimenteDisponibile.OrderByDescending(c => c.Id).ToListAsync();
            foreach (var ev in allEvents)
            {
                foreach (var userEvent in userEvents)
                {
                    if (userEvent.EvenimentDisponibilId.Equals(ev.Id))
                    {
                        myEvents.Add(ev);
                        break;
                    }
                }
            }
            return View(myEvents);
        }


        // GET: Sporuris/Create 
        public IActionResult Create()
        {
            List<string> culori = new List<string>();
            culori.Add("Default");
            culori.Add("Verde");
            culori.Add("Rosu");
            culori.Add("Albastru");
            culori.Add("Galben ");
         
            ViewData["Culori"] = culori;
            return View();
        }

        //// POST: Sporuris/Create
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nume, BackgroundColor")] EvenimentDisponibil eveniment)
        {
            if (ModelState.IsValid)
            {
                _context.EvenimenteDisponibile.Add(eveniment);
                await _context.SaveChangesAsync();
                UtilizatorEvenimentDisponibil utilizatorEvenimentDisponibil = new UtilizatorEvenimentDisponibil();
                var userId = _userManager.GetUserId(HttpContext.User);
                utilizatorEvenimentDisponibil.UtilizatorId = userId;
                utilizatorEvenimentDisponibil.EvenimentDisponibilId = eveniment.Id;
                _context.UtilizatorEvenimenteDisponibile.Add(utilizatorEvenimentDisponibil);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            List<string> culori = new List<string>();
            culori.Add("Default");
            culori.Add("Verde");
            culori.Add("Rosu");
            culori.Add("Albastru");
            culori.Add("Galben ");
            ViewData["Culori"] = culori;
            return View(eveniment);
        }

        // GET: Sporuris/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            List<string> culori = new List<string>();
            culori.Add("Default");
            culori.Add("Verde");
            culori.Add("Rosu");
            culori.Add("Albastru");
            culori.Add("Galben ");
            ViewData["Culori"] = culori;
            var eveniment = await _context.EvenimenteDisponibile.FirstOrDefaultAsync(s => s.Id == id);

            return View(eveniment);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            List<EvenimentDisponibil> myEvents = new List<EvenimentDisponibil>();
            var userId = _userManager.GetUserId(HttpContext.User);
            var userEvents = await _context.UtilizatorEvenimenteDisponibile.Where(up => up.UtilizatorId.Equals(userId)).ToListAsync();
            var allEvents = await _context.EvenimenteDisponibile.OrderByDescending(c => c.Id).ToListAsync();
            foreach (var ev in allEvents)
            {
                foreach (var userEvent in userEvents)
                {
                    if (userEvent.EvenimentDisponibilId.Equals(ev.Id))
                    {
                        myEvents.Add(ev);
                        break;
                    }
                }
            }
            return Json(new { success = true, data = myEvents });
        }

        // POST: Sporuris/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id, Nume, BackgroundColor")] EvenimentDisponibil eveniment)
        {
            if (id != eveniment.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(eveniment);
                    await _context.SaveChangesAsync();
                    
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EvenimentDisponibilExista(eveniment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            List<string> culori = new List<string>();
            culori.Add("Default");
            culori.Add("Verde");
            culori.Add("Rosu");
            culori.Add("Albastru");
            culori.Add("Galben ");
            ViewData["Culori"] = culori;
            return RedirectToAction(nameof(Index));
        }

        // GET: Sporuris/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var eveniment = await _context.EvenimenteDisponibile.FindAsync(id);
            _context.EvenimenteDisponibile.Remove(eveniment);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Delete successfully" });
        }

        private bool EvenimentDisponibilExista(int id)
        {
            return _context.EvenimenteDisponibile.Any(e => e.Id == id);
        }
    }
}
