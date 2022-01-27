using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HR.Data;
using HR.Models;
using System.Data.OleDb;
using System.Data;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using HR.ViewModels;
using OfficeOpenXml;
using System.Drawing;
using NToastNotify;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace HR.Controllers
{
    
    public class ConcediuController : Controller
    {
        #region PrivateFields
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IToastNotification _toastNotification;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly int _port;
        private readonly string _smtpServer;
        private readonly string _username;
        private readonly string _password;
        #endregion

        #region Ctor
        public ConcediuController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment,
            IToastNotification toastNotification, UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _toastNotification = toastNotification;
            _userManager = userManager;
            _port = Int32.Parse(configuration["EmailConfiguration:Port"]);
            _smtpServer = configuration["EmailConfiguration:SmtpServer"];
            _username = configuration["EmailConfiguration:Username"];
            _password = configuration["EmailConfiguration:Password"];
        }
        #endregion

        #region PublicMethods
        // GET: Concedius
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            var concedii = await _context.Concedii.OrderByDescending(c => c.Id).Where(c => c.UtilizatorId == userId).ToListAsync();
            var concediiModel = MapConcediiToViewModel(concedii);
            ViewData["Facultati"] = GetFacultiesList();
            return View(concediiModel);
        }
        [Authorize]
        public async Task<IActionResult> SearchByName(string Nume, string Prenume)
        {
            var concedii = await _context.Concedii.OrderByDescending(c => c.Id).Where(c => c.NumeAngajat == Nume && c.PrenumeAngajat == Prenume).ToListAsync();
            var concediiModel = MapConcediiToViewModel(concedii);
            ViewData["Facultati"] = GetFacultiesList();
            return View("Index", concediiModel);
        }
        [Authorize]
        public async Task<IActionResult> SearchByFaculty(string Facultate)
        {
            var concedii = await _context.Concedii.OrderByDescending(c => c.Id).Where(c => c.Facultate == Facultate).ToListAsync();
            var concediiModel = MapConcediiToViewModel(concedii);
            ViewData["Facultati"] = GetFacultiesList();
            return View("Index", concediiModel);
        }
        [Authorize]
        public async Task<IActionResult> ShowAll()
        {
            var concedii = await _context.Concedii.OrderByDescending(c => c.Id).ToListAsync();
            var concediiModel = MapConcediiToViewModel(concedii);
            ViewData["Facultati"] = GetFacultiesList();
            return View("Index", concediiModel);
        }
        [Authorize]
        // GET: Concedius/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var concediu = await _context.Concedii

                .FirstOrDefaultAsync(m => m.Id == id);
            if (concediu == null)
            {
                return NotFound();
            }

            return View(concediu);
        }


        public async Task<IActionResult> AprobaDinEmail(int? id, [FromQuery] string token)
        {
            if (id == null)
            {
                return NotFound();
            }

            var concediu = await _context.Concedii

                .FirstOrDefaultAsync(m => m.Id == id);
            if (concediu == null || concediu.Token != token)
            {
                return NotFound();
            }

            concediu.Aprobata = "Da";
            await _context.SaveChangesAsync();
            var userId = concediu.UtilizatorId;
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == user.AngajatId);
            await AprobaCerere(angajat);
            return View(angajat);
        }

        public async Task<IActionResult> RefuzaDinEmail(int? id, [FromQuery] string token)
        {
            if (id == null)
            {
                return NotFound();
            }
            var concediu = await _context.Concedii

                .FirstOrDefaultAsync(m => m.Id == id);

            if (concediu == null)
            {
                return NotFound();
            }
            return View();
        }
        public async Task<IActionResult> RefuzaCererea(int id, [Bind("ConcediuId,Motiv")] RefuzaCerereViewModel model)
        {
            if (id == 0)
            {
                return NotFound();
            }
            var concediu = await _context.Concedii

                .FirstOrDefaultAsync(m => m.Id == id);

            if (concediu == null)
            {
                return NotFound();
            }
            concediu.MotivRefuzare = model.Motiv;
            concediu.Aprobata = "Nu";
            await _context.SaveChangesAsync();
            var userId = concediu.UtilizatorId;
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == user.AngajatId);
            await RefuzaCerere(angajat);
            _toastNotification.AddSuccessToastMessage("Ai refuzat cu succes aceasta cerere.");
            return Json(new { success = true });
        }

        [Authorize]
        // GET: Concedius/Create
        public async Task<IActionResult> Create()
        {
            ViewData["AngajatId"] = new SelectList(_context.Angajat, "Id", "CNPCI");
            var concediuModel = new ConcediuViewModel();
            var userId = _userManager.GetUserId(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == user.AngajatId);
            concediuModel.NumeAngajat = angajat.Nume;
            concediuModel.PrenumeAngajat = angajat.Prenume;
            ViewData["Facultati"] = GetFacultiesList();
            return View(concediuModel);
        }
        [Authorize]
        // POST: Concedius/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NumeAngajat,PrenumeAngajat,Facultate,EmailSuperior,DataIncepere,DataSfarsit,Motiv, DataStartFinalConcediu, Comentarii")] ConcediuViewModel concediuModel, IFormFile file)
        {
            string[] interval = concediuModel.DataStartFinalConcediu.Split('-');
            var startDate = interval[0].Trim();
            var endDate = interval[1].Trim();
            CultureInfo provider = CultureInfo.InvariantCulture;
            concediuModel.DataIncepere = DateTime.ParseExact(startDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
            concediuModel.DataSfarsit = DateTime.ParseExact(endDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
            Concediu concediu = new Concediu();
            var userId = _userManager.GetUserId(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId) as UtilizatorulMeu;
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == user.AngajatId);
            var tipDocument = "";
            if (angajat != null)
            {
                if (ModelState.IsValid)
                {
                    Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = _context.Database.BeginTransaction();
                    try
                    {
                    
                        if(concediuModel.DataIncepere < DateTime.Today)
                        {
                            _toastNotification.AddErrorToastMessage("Data de incepere nu este corecta, nu poti cere un concediu pe zile din urma!");
                            ViewData["Facultati"] = GetFacultiesList();
                            return View(concediuModel);

                        }
                        if (concediuModel.DataSfarsit < DateTime.Today || concediu.DataSfarsit < concediu.DataIncepere)
                        {
                            _toastNotification.AddErrorToastMessage("Data de incepere sau de sfarsit nu este corecta!");
                            ViewData["Facultati"] = GetFacultiesList();
                            return View(concediuModel);
                        }
                        var yearStart = concediuModel.DataIncepere.Year;
                        var yearFinal = concediuModel.DataSfarsit.Year;
                        if(yearFinal - yearStart > 1)
                        {
                            _toastNotification.AddErrorToastMessage("Poti cere un concediu pe o durata maxima de 42 de zile!");
                            ViewData["Facultati"] = GetFacultiesList();
                            return View(concediuModel);
                        }
                        var numarZileLibere = await GetWorkingDays(concediuModel.DataIncepere, concediuModel.DataSfarsit);
                        if (numarZileLibere < 1)
                        {
                            _toastNotification.AddErrorToastMessage("Concediul trebuie sa fie de minim o zi!");
                            ViewData["Facultati"] = GetFacultiesList();
                            return View(concediuModel);
                        }
                        if(numarZileLibere > 42)
                        {
                            _toastNotification.AddErrorToastMessage("Poti cere un concediu pe o durata maxima de 42 de zile!");
                            ViewData["Facultati"] = GetFacultiesList();
                            return View(concediuModel);
                        }
                        if (numarZileLibere > angajat.NumarZileLibereRamase)
                        {
                            _toastNotification.AddErrorToastMessage("Nu ai suficiente zile libere ramase");
                            ViewData["Facultati"] = GetFacultiesList();
                            return View(concediuModel);
                        }

                        concediu = MapConcediuModelToConcediu(concediuModel);
                        angajat.NumarZileLibereRamase -= numarZileLibere;
                        concediu.UtilizatorId = user.Id;
                        if (file != null)
                        {
                            using (var ms = new MemoryStream())
                            {
                                file.CopyTo(ms);
                                concediu.DocumentAtasat = ms.ToArray();
                                tipDocument = Path.GetExtension(file.FileName).Replace(".", "");
                            }
                        }
                        concediu.Token = Guid.NewGuid().ToString();
                        _context.Add(concediu);
                        await _context.SaveChangesAsync();
                        var concediuId = concediu.Id;
                        await SendConfirmationEmail(concediuModel, concediu, tipDocument);
                        await transaction.CommitAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    catch
                    {
                       
                        await transaction.RollbackAsync();
                    }
                  
                }
            }


            ViewData["Facultati"] = GetFacultiesList();
            return View(concediuModel);
        }
        [Authorize]
        public async Task<int> GetWorkingDays(DateTime from, DateTime to)
        {
            var totalDays = 0;
            for (var date = from; date < to; date = date.AddDays(1))
            {
                bool isHoliday = false;
                foreach (ZiLibera holiday in await _context.ZileLibere.ToListAsync())
                {
                    if (holiday.Data == date)
                    {
                        isHoliday = true;
                        break;
                    }
                }
                if (date.DayOfWeek != DayOfWeek.Saturday
                    && date.DayOfWeek != DayOfWeek.Sunday && !isHoliday)
                    totalDays++;
            }

            return totalDays;
        }
        [Authorize]
        // GET: Concedius/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var concediu = await _context.Concedii.FindAsync(id);
            var concediuModel = MapConcediuToConcediuModel(concediu);
            if (concediuModel == null)
            {
                return NotFound();
            }
            ViewData["Facultati"] = GetFacultiesList();
            return View(concediuModel);
        }


        [Authorize]
        // GET: Concedius/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var concediu = await _context.Concedii.FindAsync(id);
            _context.Concedii.Remove(concediu);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Delete successful" });
        }

        [Authorize]
        private bool ConcediuExists(int id)
        {
            return _context.Concedii.Any(e => e.Id == id);
        }
        [Authorize]
        public async Task<IAsyncResult> ExportToExcel()
        {
            List<AngajatViewModel> emplist = _context.Angajat.Select(x => new AngajatViewModel
            {
                Nume = x.Nume,
                Prenume = x.Prenume,
                Email = x.Email,
                Sex = x.Sex,
            }).ToList();

            ExcelPackage pck = new ExcelPackage();
            ExcelWorksheet ws = pck.Workbook.Worksheets.Add("Report");

            ws.Cells["A1"].Value = "Communication";
            ws.Cells["B1"].Value = "Com1";

            ws.Cells["A2"].Value = "Report";
            ws.Cells["B2"].Value = "Report1";

            ws.Cells["A3"].Value = "Date";
            ws.Cells["B3"].Value = string.Format("{0:dd MMMM yyyy} at {0:H: mm tt}", DateTimeOffset.Now);

            ws.Cells["A6"].Value = "Nume";
            ws.Cells["B6"].Value = "Prenume";
            ws.Cells["C6"].Value = "Email";
            ws.Cells["D6"].Value = "Sex";

            int rowStart = 7;
            foreach (var item in emplist)
            {

                ws.Row(rowStart).Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                ws.Row(rowStart).Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(string.Format("pink")));


                ws.Cells[string.Format("A{0}", rowStart)].Value = item.Nume;
                ws.Cells[string.Format("B{0}", rowStart)].Value = item.Prenume;
                ws.Cells[string.Format("C{0}", rowStart)].Value = item.Email;
                ws.Cells[string.Format("D{0}", rowStart)].Value = item.Sex;
                rowStart++;
            }

            ws.Cells["A:AZ"].AutoFitColumns();
            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.Headers.Add("content-disposition", "attachment: filename=" + "ExcelReport.xlsx");
            await Response.Body.WriteAsync(pck.GetAsByteArray());
            return null;

        }

        // GET: Angajats

        #endregion

        #region privateMethods
        private static DataTable GetSchemaFromExcel(OleDbConnection excelOledbConnection)
        {
            return excelOledbConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
        }

        //Convert each datarow into employee object
        private Angajat GetEmployeeFromExcelRow(DataRow row)
        {
            return new Angajat
            {
                Nume = row[1].ToString(),
                Prenume = row[2].ToString(),
                Sex = row[3].ToString(),
            };
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
                concediuModel.MotivRefuzare = concediu.MotivRefuzare;
                concediuModel.Comentarii = concediuModel.Comentarii;
                concediiModel.Add(concediuModel);
            }
            return concediiModel;
        }
        private static Concediu MapConcediuModelToConcediu(ConcediuViewModel model)
        {
            var concediu = new Concediu();
            concediu.Aprobata = model.Aprobata;
            concediu.NumeAngajat = model.NumeAngajat;
            concediu.PrenumeAngajat = model.PrenumeAngajat;
            concediu.Facultate = model.Facultate;
            concediu.EmailSuperior = model.EmailSuperior;
            concediu.DataIncepere = model.DataIncepere;
            concediu.DataSfarsit = model.DataSfarsit;
            concediu.Motiv = model.Motiv;
            concediu.MotivRefuzare = model.MotivRefuzare;
            concediu.Comentarii = model.Comentarii;
            return concediu;
        }

        private static ConcediuViewModel MapConcediuToConcediuModel(Concediu concediu)
        {
            var model = new ConcediuViewModel();
            model.Id = concediu.Id;
            model.Aprobata = concediu.Aprobata;
            model.NumeAngajat = concediu.NumeAngajat;
            model.PrenumeAngajat = concediu.PrenumeAngajat;
            model.Facultate = concediu.Facultate;
            model.EmailSuperior = concediu.EmailSuperior;
            model.DataIncepere = concediu.DataIncepere;
            model.DataSfarsit = concediu.DataSfarsit;
            model.Motiv = concediu.Motiv;
            model.MotivRefuzare = concediu.MotivRefuzare;
            model.Comentarii = concediu.Comentarii;
            return model;
        }

        private async Task SendConfirmationEmail(ConcediuViewModel concediuModel, Concediu concediu, string tipDocument)
        {

            MimeMessage message = new MimeMessage();
            MailboxAddress from = new MailboxAddress("HRSolution",
            "emimig987@gmail.com");
            message.From.Add(from);
            MailboxAddress to = new MailboxAddress($"{ concediuModel.NumeAngajat + concediuModel.PrenumeAngajat}",
            concediuModel.EmailSuperior);
            message.To.Add(to);
            message.Subject = "Cerere Concediu";
            BodyBuilder bodyBuilder = new BodyBuilder();
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string Body = System.IO.File.ReadAllText($"{projectRootPath}/wwwroot/emailTemplate/Concediu.html");
            Body = Body.Replace("#nume#", $"{concediuModel.NumeAngajat + " " + concediuModel.PrenumeAngajat}");
            int yearDataIncepere = concediuModel.DataIncepere.Date.Year;
            int monthDataIncepere = concediuModel.DataIncepere.Date.Month;
            int dayDataIncepere = concediuModel.DataIncepere.Date.Day;
            var dataIncepere = string.Format("{0}/{1}/{2}", yearDataIncepere, monthDataIncepere, dayDataIncepere);
            int yearDataSfarsit = concediuModel.DataSfarsit.Date.Year;
            int monthDataSfarsit = concediuModel.DataSfarsit.Date.Month;
            int dayDataSfarsit = concediuModel.DataSfarsit.Date.Day;
            var dataSfarsit = string.Format("{0}/{1}/{2}", yearDataSfarsit, monthDataSfarsit, dayDataSfarsit);
            Body = Body.Replace("#dataStart#", $"{dataIncepere}");
            Body = Body.Replace("#dataSfarsit#", $"{dataSfarsit}");
            Body = Body.Replace("#tipConcediu#", $"{concediuModel.Motiv}");
            Body = Body.Replace("#id#", $"{concediu.Id}");
            Body = Body.Replace("#token#", $"{concediu.Token}");
            bodyBuilder.HtmlBody = Body;
            bodyBuilder.TextBody = "Document";
            if(concediu.DocumentAtasat != null)
            {
                if(tipDocument == "pdf")
                {
                    bodyBuilder.Attachments.Add("Document", concediu.DocumentAtasat, new ContentType("application", "pdf"));
                }
                if (tipDocument == "doc")
                {
                    bodyBuilder.Attachments.Add("Document", concediu.DocumentAtasat, new ContentType("application", "msword"));
                }
                if (tipDocument == "docx")
                {
                    bodyBuilder.Attachments.Add("Document", concediu.DocumentAtasat, new ContentType("application", "vnd.openxmlformats-officedocument.wordprocessingml.document"));
                }
            }
            message.Body = bodyBuilder.ToMessageBody();
           
            SmtpClient client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _port, true);
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
            try
            {
                System.Threading.Thread.Sleep(1000);
                await client.SendAsync(message);
            }
            catch (System.Net.Mail.SmtpFailedRecipientsException ex)
            {
                for (int i = 0; i < ex.InnerExceptions.Length; i++)
                {
                    System.Net.Mail.SmtpStatusCode status = ex.InnerExceptions[i].StatusCode;
                    if (status == System.Net.Mail.SmtpStatusCode.MailboxBusy || status == System.Net.Mail.SmtpStatusCode.MailboxUnavailable)
                    {
                        // Console.WriteLine("Delivery failed - retrying in 5 seconds.");
                        System.Threading.Thread.Sleep(3000);
                        client.Send(message);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            await client.DisconnectAsync(true);
            client.Dispose();
        }

        private async Task RefuzaCerere(Angajat angajat)
        {
            MimeMessage message = new MimeMessage();
            MailboxAddress from = new MailboxAddress("HRSolution",
            "emimig987@gmail.com");
            message.From.Add(from);
            MailboxAddress to = new MailboxAddress($"{ angajat.Nume + angajat.Prenume}",
            angajat.EmailInstitutional);
            message.To.Add(to);
            message.Subject = "Cerere Concediu";
            BodyBuilder bodyBuilder = new BodyBuilder();
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string Body = System.IO.File.ReadAllText($"{projectRootPath}/wwwroot/emailTemplate/CerereRefuzata.html");
            bodyBuilder.HtmlBody = Body;
            bodyBuilder.TextBody = "Hello World!";
            message.Body = bodyBuilder.ToMessageBody();
            SmtpClient client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _port, true);
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
            try
            {
                System.Threading.Thread.Sleep(1000);
                await client.SendAsync(message);
            }
            catch (System.Net.Mail.SmtpFailedRecipientsException ex)
            {
                for (int i = 0; i < ex.InnerExceptions.Length; i++)
                {
                    System.Net.Mail.SmtpStatusCode status = ex.InnerExceptions[i].StatusCode;
                    if (status == System.Net.Mail.SmtpStatusCode.MailboxBusy || status == System.Net.Mail.SmtpStatusCode.MailboxUnavailable)
                    {
                        // Console.WriteLine("Delivery failed - retrying in 5 seconds.");
                        System.Threading.Thread.Sleep(3000);
                        client.Send(message);
                    }
                    else
                    {
                        //  Console.WriteLine("Failed to deliver message to {0}", ex.InnerExceptions[i].FailedRecipient);
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                //  Console.WriteLine("Exception caught in RetryIfBusy(): {0}",ex.ToString());
                throw ex;
            }
            await client.DisconnectAsync(true);
            client.Dispose();
        }

        private async Task AprobaCerere(Angajat angajat)
        {
            MimeMessage message = new MimeMessage();
            MailboxAddress from = new MailboxAddress("HRSolution",
            "emimig987@gmail.com");
            message.From.Add(from);
            MailboxAddress to = new MailboxAddress($"{ angajat.Nume + angajat.Prenume}",
            angajat.EmailInstitutional);
            message.To.Add(to);
            message.Subject = "Cerere Concediu";
            BodyBuilder bodyBuilder = new BodyBuilder();
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string Body = System.IO.File.ReadAllText($"{projectRootPath}/wwwroot/emailTemplate/CerereAprobata.html");
            bodyBuilder.HtmlBody = Body;
            bodyBuilder.TextBody = "Hello World!";
            message.Body = bodyBuilder.ToMessageBody();
            SmtpClient client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _port, true);
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
            try
            {
                System.Threading.Thread.Sleep(1000);
                await client.SendAsync(message);
            }
            catch (System.Net.Mail.SmtpFailedRecipientsException ex)
            {
                for (int i = 0; i < ex.InnerExceptions.Length; i++)
                {
                    System.Net.Mail.SmtpStatusCode status = ex.InnerExceptions[i].StatusCode;
                    if (status == System.Net.Mail.SmtpStatusCode.MailboxBusy || status == System.Net.Mail.SmtpStatusCode.MailboxUnavailable)
                    {
                        // Console.WriteLine("Delivery failed - retrying in 5 seconds.");
                        System.Threading.Thread.Sleep(3000);
                        client.Send(message);
                    }
                    else
                    {
                        //  Console.WriteLine("Failed to deliver message to {0}", ex.InnerExceptions[i].FailedRecipient);
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                //  Console.WriteLine("Exception caught in RetryIfBusy(): {0}",ex.ToString());
                throw ex;
            }
            await client.DisconnectAsync(true);
            client.Dispose();
        }

        [HttpGet]
        public IActionResult GetName(string term)
        {

            var result = _context.Angajat.Where(s => s.Nume.Contains(term) || s.Prenume.Contains(term))
                         .Select(s => s.Nume);
            return Json(result);
        }

        public List<string> GetFacultiesList()
        {
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
            facultati.Add("Horticultură");
            facultati.Add("Ingineria si Managementul Sistemelor Tehnologice Drobeta - Turnu Severin");
            facultati.Add("Inginerie Electrică");
            facultati.Add("Litere Drobeta Turnu Severin");
            facultati.Add("Litere");
            facultati.Add("Mecanică");
            facultati.Add("Științe");
            facultati.Add("Științe Sociale");
            facultati.Add("Teologie Ortodoxă");
            facultati.Add("Pregătirea Personalului Didactic");
            facultati.Add("Pregătirea Personalului Didactic Drobeta - Turnu Severin");
            facultati.Add("Școala Doctorală Academician Radu Voinea");
            facultati.Add("Școala Doctorală a Facultății de Drept");
            facultati.Add("Școala Doctorală Alexandru Piru");
            facultati.Add("Școala Doctorală Constantin Belea");
            facultati.Add("Școala Doctorală de Inginerie a Resurselor Animale/Vegetale");
            facultati.Add("Școala Doctorală de Inginerie Electrică și Energetică ");
            facultati.Add("Școala Doctorală de Științe");
            facultati.Add("Școala Doctorală de Științe Economice");
            facultati.Add("Școala Doctorală de Științe Sociale și Umaniste");
            facultati.Add("Școala Doctorală de Teologie Ortodoxă Sfântul Nicodim");
            return facultati;
        }
        #endregion

    }
}
