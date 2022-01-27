using HR.Data;
using HR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class PlanificatorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        private readonly UserManager<IdentityUser> _userManager;
        public PlanificatorController(ApplicationDbContext context, IToastNotification toastNotification, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _toastNotification = toastNotification;
            _userManager = userManager;
        }



        public async Task<IActionResult> AfiseazaPlanificator()
        {
            List<EvenimentDisponibil> myEvents = new List<EvenimentDisponibil>();
            var userId = _userManager.GetUserId(HttpContext.User);
            var userEvents = await _context.UtilizatorEvenimenteDisponibile.Where(up => up.UtilizatorId.Equals(userId)).ToListAsync();
            var allEvents = await _context.EvenimenteDisponibile.ToListAsync();
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

        [HttpGet]
        public  async Task<IActionResult> ShowErrorToaster()
        {
            _toastNotification.AddErrorToastMessage("Nu poti planifica un element in trecut!");
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> PlanificaEveniment(string eveniment)
        {
            var evenimentToAdd = JsonConvert.DeserializeObject<EvenimentPlanificat>(eveniment);
            evenimentToAdd.DataInceput = evenimentToAdd.DataInceput.Date.AddDays(1);
            evenimentToAdd.DataSfarsit = evenimentToAdd.DataInceput.Date.AddDays(1); ;
            _context.EvenimentePlanificate.Add(evenimentToAdd);
            await _context.SaveChangesAsync();
            UtilizatorEvenimentPlanificat utilizatorEvenimentPlanificat = new UtilizatorEvenimentPlanificat();
            var userId = _userManager.GetUserId(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            utilizatorEvenimentPlanificat.UtilizatorId = userId;
            utilizatorEvenimentPlanificat.EvenimentPlanificatId = evenimentToAdd.Id;
            _context.UtilizatorEvenimentePlanificate.Add(utilizatorEvenimentPlanificat);
            if (evenimentToAdd.DataInceput.Date.AddDays(1) == DateTime.Today)
            {
                user.NumarEvenimentePlanificateAzi++;
            }
            await _context.SaveChangesAsync();
            _toastNotification.AddSuccessToastMessage("Evenimentul a fost adaugat cu success!");
            return Json(new { success = true });
        }

    }
}
