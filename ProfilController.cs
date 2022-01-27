using HR.Data;
using HR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [Authorize]
    public class ProfilController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IToastNotification _toastNotification;
        public ProfilController(ApplicationDbContext context, UserManager<IdentityUser> userManager,
            IToastNotification toastNotification
            )
        {
            _context = context;
            _userManager = userManager;
            _toastNotification = toastNotification;
        }

        public async Task<IActionResult> Index()
        {
            var year = DateTime.Now.Year;
            ViewBag.AnMinus5 = $"{year - 5}";
            ViewBag.AnMinus4 = $"{year - 4}";
            ViewBag.AnMinus3 = $"{year - 3}";
            ViewBag.AnMinus2 = $"{year - 2}";
            ViewBag.AnMinus1 = $"{year - 1}";
            var userId = _userManager.GetUserId(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            var angajat = await _context.Angajat.Include(a => a.Familie)
                .Include(a => a.EvolutiePersonala).Include(a => a.EvolutieLocDeMunca).Include(a => a.Salariu).FirstOrDefaultAsync(a => a.Id == user.AngajatId);
            return View(angajat);
        }

        public async Task<IActionResult> UpdateProfile(Angajat angajat)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            var angajatDb = await _context.Angajat.Include(a => a.Familie)
                .Include(a => a.EvolutiePersonala).Include(a => a.EvolutieLocDeMunca).Include(a => a.Salariu).FirstOrDefaultAsync(a => a.Id == user.AngajatId);
            angajatDb.Email = angajat.Email;
            angajatDb.DomiciliuTelefon = angajat.DomiciliuTelefon;
            _context.Update(angajatDb);
            await _context.SaveChangesAsync();
            _toastNotification.AddSuccessToastMessage("Datele au fost modificate cu succes!");
            return RedirectToAction("Index");
        }

    }
}