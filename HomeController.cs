using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NewsAPI;
using NewsAPI.Models;
using NewsAPI.Constants;
using Microsoft.AspNetCore.Authorization;
using HR.Data;
using Microsoft.AspNetCore.Identity;
using HR.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HR.ViewModels;

namespace HR.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index(string word)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            if (await _userManager.IsInRoleAsync(user, "Cadru Didactic"))
            {
                return RedirectToAction("Index", "Profil");
            }
            var articles = new List<Article>();
            var newsApiClient = new NewsApiClient("38918f485edc45e6b3cb63db3ab3630d");
            var articlesResponse = await newsApiClient.GetTopHeadlinesAsync(new TopHeadlinesRequest
            {
                Q = "Employees",
                Language = Languages.EN,
            });
            if (user.SetareHomePage != null)
            {
                switch (user.SetareHomePage.ToUpperInvariant())
                {
                    case "DASHBOARD":
                        return RedirectToAction("Dashboard", "Home");
                    case "NEWS":

                        if (articlesResponse.Status == Statuses.Ok)
                        {
                            articles = articlesResponse.Articles;
                            return View("News", articles);
                        }
                        else
                        {
                            return View();
                        }
                    case "ANGAJATI":
                        List<Angajat> angajati = new List<Angajat>();
                        angajati = await _context.Angajat.ToListAsync();
                        return RedirectToAction("Index", "Angajat", angajati);
                    case "CONCEDII":
                        var concedii = await _context.Concedii.ToListAsync();
                        var concediiModel = MapConcediiToViewModel(concedii);
                        return RedirectToAction("Index", "Concediu", concedii);
                    case "SALVARE SITUATIE":
                        List<string> luni = new List<string>();
                        luni.Add("Ianuarie");
                        luni.Add("Februarie");
                        luni.Add("Martie");
                        luni.Add("Aprilie");
                        luni.Add("Mai");
                        luni.Add("Iunie");
                        luni.Add("Iulie");
                        luni.Add("August");
                        luni.Add("Septembrie");
                        luni.Add("Octombrie");
                        luni.Add("Noiembrie");
                        luni.Add("Decembrie");
                        ViewData["Luni"] = new SelectList(luni);
                        List<string> ani = new List<string>();
                        ani.Add("2020");
                        ani.Add("2021");
                        ani.Add("2022");
                        ani.Add("2023");
                        ani.Add("2024");
                        ViewData["Ani"] = new SelectList(ani);
                        return RedirectToAction("SituatieLunara", "SituatieLunara");
                    case "STATISTICI":
                        return View("Statistici");
                    case "PLANIFICATOR":
                        var evenimenteDisponibile = await _context.EvenimenteDisponibile.ToListAsync();
                        return View("AfiseazaPlanificator", evenimenteDisponibile);
                    case "SETARI SALARII":
                        var applicationDbContext = _context.Salariu.Include(s => s.Angajat);
                        return RedirectToAction("Index", "Salariu", applicationDbContext);
                    default:
                        return null;
                }
            }
            if (articlesResponse.Status == Statuses.Ok)
            {
                articles = articlesResponse.Articles;
                return View("News", articles);
            }
            return RedirectToAction("Index", "Angajati");
        }

        public async Task<IActionResult> Dashboard()
        {
            List<EvenimentPlanificat> todayEvents = new List<EvenimentPlanificat>();
            List<EvenimentPlanificat> thisWeekEvents = new List<EvenimentPlanificat>();
            var userId = _userManager.GetUserId(HttpContext.User);
            var userEvents = await _context.UtilizatorEvenimentePlanificate.Where(up => up.UtilizatorId.Equals(userId)).ToListAsync();
            var allEvents = await _context.EvenimentePlanificate.ToListAsync();
            foreach (var ev in allEvents)
            {
                foreach (var userEvent in userEvents)
                {
                    if (userEvent.EvenimentPlanificatId.Equals(ev.Id) && ev.DataInceput.Date == DateTime.Today)
                    {
                        todayEvents.Add(ev);
                        break;
                    }
                }
            }
            DateTime Today = DateTime.Today;
            DateTime StartDate = Today.AddDays(-((int)Today.DayOfWeek));
            DateTime EndDate = StartDate.AddDays(7).AddSeconds(-1);
            foreach (var ev in allEvents)
            {
                foreach (var userEvent in userEvents)
                {
                    if (userEvent.EvenimentPlanificatId.Equals(ev.Id) && userEvent.EvenimentPlanificat.DataInceput >= StartDate && userEvent.EvenimentPlanificat.DataSfarsit <= EndDate)
                    {
                        thisWeekEvents.Add(ev);
                        break;
                    }
                }
            }
            
            var dashboardModel = new DashboardViewModel();
            var evenimentePlanificate = todayEvents;
            dashboardModel.EvenimentePlanificate = evenimentePlanificate;
            dashboardModel.ThisWeekEvents = thisWeekEvents;
            dashboardModel.NumarCadreDidacticeInregistrate = _context.Angajat.Where(a => a.FunctieDeConducere == false).Count();
            dashboardModel.NumarFunctiiConducereInregistrate = _context.Angajat.Where(a => a.FunctieDeConducere == true).Count();
            dashboardModel.NumarAsistenti = _context.Angajat.Where(a => a.FunctieDidactica == "Asistent Universitar").Count();
            dashboardModel.NumarSefiLucrari = _context.Angajat.Where(a => a.FunctieDidactica == "Sef lucrări(lector universitar)").Count();
            dashboardModel.NumarConferentiari = _context.Angajat.Where(a => a.FunctieDidactica == "Conferentiar universitar").Count();
            dashboardModel.NumarProfesori = _context.Angajat.Where(a => a.FunctieDidactica == "Profesor universitar").Count();
            dashboardModel.NumarDecani = _context.Angajat.Where(a => a.Functia == "Decan").Count();
            dashboardModel.NumarProdecani = _context.Angajat.Where(a => a.Functia == "Prodecan").Count();
            dashboardModel.NumarDirectoriDeDepartament = _context.Angajat.Where(a => a.Functia == "Director de departament").Count();
            dashboardModel.NumarDirectoriGeneraliAdministrativi = _context.Angajat.Where(a => a.Functia == "Director general adjunct administrativ al universitatii").Count();
            return View(dashboardModel);
        }

        public async Task<IActionResult> GetNews(string word)
        {
            if (word == null)
            {
                word = "Employees";
            }
            var articles = new List<Article>();
            var newsApiClient = new NewsApiClient("38918f485edc45e6b3cb63db3ab3630d");
            var articlesResponse = await newsApiClient.GetTopHeadlinesAsync(new TopHeadlinesRequest
            {
                Q = word,
                Language = Languages.EN,
            });
            if (articlesResponse.Status == Statuses.Ok)
            {
                articles = articlesResponse.Articles;
                return RedirectToAction("Index", articles);
            }
            else
            {
                return View("Index");
            }
        }
        private static List<ConcediuViewModel> MapConcediiToViewModel(List<Concediu> concedii)
        {
            var concediiModel = new List<ConcediuViewModel>();
            foreach (var concediu in concedii)
            {
                var concediuModel = new ConcediuViewModel();
                concediuModel.Id = concediu.Id;
                concediuModel.Aprobata = concediu.Aprobata;
                concediuModel.NumeAngajat = concediu.NumeAngajat;
                concediuModel.PrenumeAngajat = concediu.PrenumeAngajat;
                concediuModel.Facultate = concediu.Facultate;
                concediuModel.EmailSuperior = concediu.EmailSuperior;
                concediuModel.DataIncepere = concediu.DataIncepere;
                concediuModel.DataSfarsit = concediu.DataSfarsit;
                concediuModel.Motiv = concediu.Motiv;
                concediiModel.Add(concediuModel);
            }
            return concediiModel;
        }
    }
}