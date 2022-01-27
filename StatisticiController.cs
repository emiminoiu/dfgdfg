using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HR.Data;
using HR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class StatisticiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticiController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            List<StatisticiViewModel> statistici = new List<StatisticiViewModel>();
            var asistentiUniversitari = await _context.Angajat.Where(a => a.FunctieDidactica == "Asistent Universitar").ToListAsync();
            var numarAsistenti = asistentiUniversitari.Count();
            statistici.Add(new StatisticiViewModel
            {
                FunctieDidactica = "Asistent Universitar",
                NumarAnajati = numarAsistenti
            });
            var lectoriUniversitari = await _context.Angajat.Where(a => a.FunctieDidactica == "Sef lucrări (lector universitar)").ToListAsync();
            var numarLectori = lectoriUniversitari.Count();
            statistici.Add(new StatisticiViewModel
            {
                FunctieDidactica = "Sef lucrări(lector universitar)",
                NumarAnajati = numarLectori
            });
            var conferentiariUniversitari = await _context.Angajat.Where(a => a.FunctieDidactica == "Conferentiar universitar").ToListAsync();
            var numarConferentiari = conferentiariUniversitari.Count();
            statistici.Add(new StatisticiViewModel
            {
                FunctieDidactica = "Conferentiar universitar",
                NumarAnajati = numarConferentiari
            });
            var profesoriUniversitari = await _context.Angajat.Where(a => a.FunctieDidactica == "Profesor universitar").ToListAsync();
            var numarProfesori = profesoriUniversitari.Count();
            statistici.Add(new StatisticiViewModel
            {
                FunctieDidactica = "Profesor universitar",
                NumarAnajati = numarProfesori
            });

            return Json(new { success = true, data = statistici });
        }

        [HttpGet]
        public async Task<IActionResult> GetStatisticsPerLeadershipFunction()
        {
            List<StatisticsPerLeadershipFunctions> statistici = new List<StatisticsPerLeadershipFunctions>();
            var rectoriUniversitari = await _context.Angajat.Where(a => a.Functia == "Rector").ToListAsync();
            var numarRectori = rectoriUniversitari.Count();
            statistici.Add(new StatisticsPerLeadershipFunctions
            {
                FunctieConducere = "Rector",
                NumarAnajati = numarRectori
            });
            var prorectoriUniversitari = await _context.Angajat.Where(a => a.Functia == "Prorector").ToListAsync();
            var numarProRectori = prorectoriUniversitari.Count();
            statistici.Add(new StatisticsPerLeadershipFunctions
            {
                FunctieConducere = "Prorector",
                NumarAnajati = numarProRectori
            });

            var directorigeneraliadministrativiUniversitari = await _context.Angajat.Where(a => a.Functia == "Director general administrativ al universitatii").ToListAsync();
            var numardirectorigeneraliadministrativiUniversitari = directorigeneraliadministrativiUniversitari.Count();
            statistici.Add(new StatisticsPerLeadershipFunctions
            {
                FunctieConducere = "Director general administrativ al universitatii",
                NumarAnajati = numardirectorigeneraliadministrativiUniversitari
            });

            var decani = await _context.Angajat.Where(a => a.Functia == "Decan").ToListAsync();
            var numarDecani = decani.Count();
            statistici.Add(new StatisticsPerLeadershipFunctions
            {
                FunctieConducere = "Decan",
                NumarAnajati = numarDecani
            });

            var prodecaniUniversitari = await _context.Angajat.Where(a => a.Functia == "Prodecan").ToListAsync();
            var numarProdecani = prodecaniUniversitari.Count();
            statistici.Add(new StatisticsPerLeadershipFunctions
            {
                FunctieConducere = "Prodecan",
                NumarAnajati = numarProdecani
            });

            var directoriDeDepartamentUniversitari = await _context.Angajat.Where(a => a.Functia == "Director de departament").ToListAsync();
            var numardirectoriDeDepartamentUniversitari = directoriDeDepartamentUniversitari.Count();
            statistici.Add(new StatisticsPerLeadershipFunctions
            {
                FunctieConducere = "Director de departament",
                NumarAnajati = numardirectoriDeDepartamentUniversitari
            });

            var directorigeneraliadjunctAdministrativi = await _context.Angajat.Where(a => a.Functia == "Director general adjunct administrativ al universitatii").ToListAsync();
            var numardirectorigeneraliadjunctAdministrativi = directorigeneraliadjunctAdministrativi.Count();
            statistici.Add(new StatisticsPerLeadershipFunctions
            {
                FunctieConducere = "Director general adjunct administrativ al universitatii",
                NumarAnajati = numardirectorigeneraliadjunctAdministrativi
            });

            return Json(new { success = true, data = statistici });
        }



        [HttpGet]
        public async Task<IActionResult> GetStatisticsPerFunctions()
        {
            List<StatisticiFunctiiConducereViewModel> statisticiFunctiiConducere = new List<StatisticiFunctiiConducereViewModel>();
            for(var i = 1; i <= 12; i++)
            {
                var statistica = new StatisticiFunctiiConducereViewModel();
                statistica.Luna = i;
                var angajati = await _context.Angajat.Where(a => a.Functia != null && a.CreatedAt.Month == i).ToListAsync();
                statistica.NumarAngajati = angajati.Count();
                statisticiFunctiiConducere.Add(statistica);
            }
            List<StatisticiFunctiiDidacticeViewModel> statisticiFunctiiDidactice = new List<StatisticiFunctiiDidacticeViewModel>();
            for (var i = 1; i <= 12; i++)
            {
                var statistica = new StatisticiFunctiiDidacticeViewModel();
                statistica.Luna = i;
                var angajati = await _context.Angajat.Where(a => a.FunctieDidactica != null && a.CreatedAt.Month == i).ToListAsync();
                statistica.NumarAngajati = angajati.Count();
                statisticiFunctiiDidactice.Add(statistica);
            }
            return Json(new { functiiConducere = statisticiFunctiiConducere, functiiDidactice = statisticiFunctiiDidactice });
        }

        [HttpGet]
        public async Task<IActionResult> CereriDeConcediuPerFacultate()
        {
            List<CereriConcediuPerFacultateViewModel> statistici = new List<CereriConcediuPerFacultateViewModel>();
            List<string> facultati = new List<string>();
            facultati.Add("Agronomie");
            facultati.Add("Automatica, Calculatoare si Electronica");
            facultati.Add("Drept");
            facultati.Add("Drept Drobeta Turnu-Severin");
            facultati.Add("Economie și Administrarea Afacerilor Drobeta Tr. Severin");
            facultati.Add("Economie și Administrarea Afacerilor");
            facultati.Add("Educație Fizică și Sport");
            facultati.Add("Educație Fizică și Sport Drobeta Turnu Severin");
            facultati.Add("Electromecanică, Mediu și Informatică Aplicată");
            facultati.Add("Ingineria si Managementul Sistemelor Tehnologice Drobeta - Turnu Severin");
            facultati.Add("Inginerie Electrică");
            facultati.Add("Litere Drobeta Turnu Severin");
            facultati.Add("Litere");
            facultati.Add("Mecanică");
            facultati.Add("Științe");
            facultati.Add("Științe Sociale");
            facultati.Add("Pregătirea Personalului Didactic");
            facultati.Add("Pregătirea Personalului Didactic Drobeta - Turnu Severin");
       
            foreach(var facultate in facultati)
            {
                var statistica = new CereriConcediuPerFacultateViewModel();
                statistica.Facultate = facultate;
                statistica.NumarCereri = await _context.Concedii.Where(c => c.Facultate == facultate).CountAsync();
                statistici.Add(statistica);
            } 
            return Json(new { statistici });
        }
    }
}