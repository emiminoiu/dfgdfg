

using HR.Data;
using HR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class RaportController : Controller
    {
        #region privateFields
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        #endregion

        #region Constructor
        public RaportController(ApplicationDbContext _context, IToastNotification _toastNotification)
        {
            this._context = _context;
            this._toastNotification = _toastNotification;
        }
        #endregion
        #region publicMethods
        public IActionResult GenerareRaport()
        {
            List<string> tipuriMarire = new List<string>();
            tipuriMarire.Add("Procent");
            tipuriMarire.Add("Suma");
            ViewData["TipMarire"] = new SelectList(tipuriMarire);
            return View();
        }
        public IActionResult GenerareStatistica()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerareRaport([FromForm] RaportViewModel raport)
        {
            var salarii = await _context.Salariu.ToListAsync();
            if (raport.Procent != null)
            {
                foreach (var salariu in salarii)
                {
                    salariu.SalariuBrut += salariu.SalariuBrut * ((decimal)raport.Procent / 100);
                }
            }
            if (raport.Suma != null)
            {
                foreach (var salariu in salarii)
                {
                    salariu.SalariuBrut += raport.Suma;
                }
            }
            await _context.SaveChangesAsync();
            _toastNotification.AddSuccessToastMessage("Raportul a fost generat cu succes!");
            return RedirectToAction("GenerareRaport");
        }


        #endregion
    }
}
