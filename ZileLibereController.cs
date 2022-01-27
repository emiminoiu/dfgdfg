using HR.Data;
using HR.Models;
using HR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class ZileLibereController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public ZileLibereController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: Sporuris
        public async Task<IActionResult> Index()
        {
            var functiiConducere = await _context.ZileLibere.OrderByDescending(f => f.Id).ToListAsync();
            return View(functiiConducere);
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
        public async Task<IActionResult> Create([Bind("Nume, Data")] ZiLibera ziLibera)
        {
            if (ModelState.IsValid)
            {
                _context.ZileLibere.Add(ziLibera);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(ziLibera);
        }

        // GET: Sporuris/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ziLibera = await _context.ZileLibere.FirstOrDefaultAsync(s => s.Id == id);

            return View(ziLibera);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var zileLibere = await _context.ZileLibere.OrderByDescending(f => f.Id).ToListAsync();
            var zileLibereFormatCorect = new List<ZiLiberaViewModel>();
            foreach (var ziLibera in zileLibere)
            {
                var ziLiberaModel = new ZiLiberaViewModel();
                int year = ziLibera.Data.Year;
                int month = ziLibera.Data.Month;
                int day = ziLibera.Data.Day;
                ziLiberaModel.Data = string.Format("{0}/{1}/{2}", year, month, day);
                ziLiberaModel.Id = ziLibera.Id;
                ziLiberaModel.Nume = ziLibera.Nume;
                zileLibereFormatCorect.Add(ziLiberaModel);
            }
            return Json(new { success = true, data = zileLibereFormatCorect });
        }

        // POST: Sporuris/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id, Nume, Data")] ZiLibera ziLibera)
        {
            if (id != ziLibera.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ziLibera);
                    await _context.SaveChangesAsync();

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ZiLiberaExista(ziLibera.Id))
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
            var ziLibera = await _context.ZileLibere.FindAsync(id);
            _context.ZileLibere.Remove(ziLibera);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Delete successful" });
        }

        private bool ZiLiberaExista(int id)
        {
            return _context.ZileLibere.Any(e => e.Id == id);
        }
    }
}
