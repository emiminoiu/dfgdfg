using HR.Data;
using HR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class EvenimentePlanificate : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public EvenimentePlanificate(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        // GET: Sporuris
        public async Task<IActionResult> Index()
        {
            List<EvenimentPlanificat> myEvents = new List<EvenimentPlanificat>();
            var userId = _userManager.GetUserId(HttpContext.User);
            var userEvents = await _context.UtilizatorEvenimentePlanificate.Where(up => up.UtilizatorId.Equals(userId)).OrderByDescending(c => c.Id).ToListAsync();
            var allEvents = await _context.EvenimentePlanificate.ToListAsync();
            foreach (var ev in allEvents)
            {
                foreach (var userEvent in userEvents)
                {
                    if (userEvent.EvenimentPlanificatId.Equals(ev.Id))
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
            return View();
        }

        //// POST: Sporuris/Create
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nume, DataInceput,DataSfarsit")] EvenimentPlanificat eveniment)
        {
            if (ModelState.IsValid)
            {
                _context.EvenimentePlanificate.Add(eveniment);
                await _context.SaveChangesAsync();
                UtilizatorEvenimentPlanificat utilizatorEvenimentPlanificat = new UtilizatorEvenimentPlanificat();
                var userId = _userManager.GetUserId(HttpContext.User);
                var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
                utilizatorEvenimentPlanificat.UtilizatorId = userId;
                utilizatorEvenimentPlanificat.EvenimentPlanificatId = eveniment.Id;
                _context.UtilizatorEvenimentePlanificate.Add(utilizatorEvenimentPlanificat);
                if(eveniment.DataInceput.Date == DateTime.Today)
                {
                    user.NumarEvenimentePlanificateAzi++;
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(eveniment);
        }

        // GET: Sporuris/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eveniment = await _context.EvenimentePlanificate.FirstOrDefaultAsync(s => s.Id == id);

            return View(eveniment);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            List<EvenimentPlanificat> myEvents = new List<EvenimentPlanificat>();
            var userId = _userManager.GetUserId(HttpContext.User);
            var userEvents = await _context.UtilizatorEvenimentePlanificate.Where(up => up.UtilizatorId.Equals(userId)).ToListAsync();
            var allEvents = await _context.EvenimentePlanificate.OrderByDescending(c => c.Id).ToListAsync();
            foreach (var ev in allEvents)
            {
                foreach (var userEvent in userEvents)
                {
                    if (userEvent.EvenimentPlanificatId.Equals(ev.Id))
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
        public async Task<IActionResult> Edit(int id, [Bind("Id, Nume, DataInceput,DataSfarsit")] EvenimentPlanificat eveniment)
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
                    if (!EvenimentPlanificatExista(eveniment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Sporuris/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var eveniment = await _context.EvenimentePlanificate.FindAsync(id);
            var userId = _userManager.GetUserId(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            if (eveniment.DataInceput.Date == DateTime.Today)
            {
                user.NumarEvenimentePlanificateAzi--;
            }
            _context.EvenimentePlanificate.Remove(eveniment);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Delete successful" });
        }

        private bool EvenimentPlanificatExista(int id)
        {
            return _context.EvenimentePlanificate.Any(e => e.Id == id);
        }
    }
}
