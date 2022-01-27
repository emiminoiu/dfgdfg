using System.Collections.Generic;
using System.Threading.Tasks;
using HR.Data;
using HR.Models;
using HR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class SetariController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        private readonly UserManager<IdentityUser> _userManager;
        public SetariController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IToastNotification toastNotification)
        {
            _context = context;
            _userManager = userManager;
            _toastNotification = toastNotification;
        }
        public async Task<IActionResult> Index()
        {
            var cadreDidactice = new List<CadruDidacticManagementViewModel>();
            var conturiSuspendate = new List<CadruDidacticManagementViewModel>();

            foreach (IdentityUser utilizator in await _userManager.Users.ToListAsync())
            {
                var user = await _userManager.FindByIdAsync(utilizator.Id) as UtilizatorulMeu;
               
                if(user != null)
                {
                    if (user.AngajatId != null && user.EmailConfirmed)
                    {
                        var cadruDidacticModel = new CadruDidacticManagementViewModel();
                        var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == user.AngajatId);
                        cadruDidacticModel.NumeComplet = $"{angajat.Nume} {angajat.Prenume}";
                        cadruDidacticModel.UtilizatorulMeu = user;
                        cadreDidactice.Add(cadruDidacticModel);
                    }

                    if (user.AngajatId != null && !user.EmailConfirmed)
                    {
                        var cadruDidacticSuspendat = new CadruDidacticManagementViewModel();
                        var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == user.AngajatId);
                        cadruDidacticSuspendat.NumeComplet = $"{angajat.Nume} {angajat.Prenume}";
                        cadruDidacticSuspendat.UtilizatorulMeu = user;
                        conturiSuspendate.Add(cadruDidacticSuspendat);
                    }
                }
            }
            var utilizatori = new SetariViewModel();
            var setari = await _context.Setari.FirstOrDefaultAsync(i => i.Id == 1);
            if(setari != null)
            {
                utilizatori.CheieInregistrare = setari.CheieInregistrare;
            }
            else
            {
                utilizatori.CheieInregistrare = "";
            }
            utilizatori.CadreDidactice = cadreDidactice;
            utilizatori.ConturiSuspendate = conturiSuspendate;
            return View(utilizatori);
        }
        [HttpPost]
        public async Task<IActionResult> SalveazaSetareaPentruHomePage(string selectedOption)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            user.SetareHomePage = selectedOption;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> ReseteazaZileleLibere()
        {
            foreach (var angajat in await _context.Angajat.ToListAsync())
            {
                angajat.NumarZileLibereRamase = 42;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Zilele au fost resetate cu succes" });
        }

        public async Task<IActionResult> Suspenda(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            user.EmailConfirmed = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Contul a fost suspendat cu succes" });
        }

        public async Task<IActionResult> Deblocheaza(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            user.EmailConfirmed = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Contul a fost deblochat cu succes" });
        }

        public async Task<IActionResult> Sterge(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            await _userManager.DeleteAsync(user);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Contul a fost sters cu succes" });
        }

        public async Task<IActionResult> SeteazaCheiaUnica(SetariViewModel setari)
        {
            var cheie = await _context.Setari.FirstOrDefaultAsync(i => i.Id == 1);
            if(cheie == null)
            {
                _context.Setari.Add(new Setari
                {
                    CheieInregistrare = setari.CheieInregistrare
                });

            }
            else
            {
                cheie.CheieInregistrare = setari.CheieInregistrare;
            }
            await _context.SaveChangesAsync();
            _toastNotification.AddSuccessToastMessage("Cheia de inregistrare a fost modificata cu success!");
            return RedirectToAction("Index");
        }


    }
}