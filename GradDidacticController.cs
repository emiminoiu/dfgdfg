using HR.Data;
using HR.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class GradDidacticController : Controller
    {
        private static int AngajatId;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly int _port;
        private readonly string _smtpServer;
        private readonly string _username;
        private readonly string _password;
        public GradDidacticController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment, IConfiguration configuration)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _port = Int32.Parse(configuration["EmailConfiguration:Port"]);
            _smtpServer = configuration["EmailConfiguration:SmtpServer"];
            _username = configuration["EmailConfiguration:Username"];
            _password = configuration["EmailConfiguration:Password"];
        }

        // GET: Sporuris
        public async Task<IActionResult> Index()
        {
            var gradeDidactice = await _context.GradeDidactice.OrderByDescending(c => c.Id).ToListAsync();
            return View(gradeDidactice);
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
        public async Task<IActionResult> Create([Bind("Nume, Gradatia, SalariuBaza")] GradDidactic gradDidactic)
        {
            if (ModelState.IsValid)
            {
                _context.Add(gradDidactic);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(gradDidactic);
        }

        // GET: Sporuris/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gradDidactic = await _context.GradeDidactice.FirstOrDefaultAsync(s => s.Id == id);

            return View(gradDidactic);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Json(new { success = true, data = await _context.GradeDidactice.OrderByDescending(c => c.Id).ToListAsync() });
        }

        // POST: Sporuris/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id, Nume, Gradatia, SalariuBaza, IntervalAni")]GradDidactic gradDidactic)
        {
            if (id != gradDidactic.Id)
            {
                return NotFound();
            }
            var gradDidacticInitial = await _context.GradeDidactice.FirstOrDefaultAsync(g => g.Id == id);
            var local = _context.Set<GradDidactic>()
           .Local
           .FirstOrDefault(entry => entry.Id.Equals(gradDidacticInitial.Id));
            _context.Entry(local).State = EntityState.Detached;
            _context.Entry(gradDidacticInitial).State = EntityState.Detached;
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gradDidactic);
                    await _context.SaveChangesAsync();
                    if(gradDidacticInitial.SalariuBaza != gradDidactic.SalariuBaza)
                    {
                        var angajati = _context.Angajat.Where(a => a.GradDidacticId == gradDidactic.Id);
                        foreach (var angajat in angajati)
                        {
                            await SendConfirmationEmail(angajat);
                        }
                    }
                 
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GradExists(gradDidactic.Id))
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
            var gradDidactic = await _context.GradeDidactice.FindAsync(id);
            _context.GradeDidactice.Remove(gradDidactic);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Delete successful" });
        }

      

        private bool GradExists(int id)
        {
            return _context.GradeDidactice.Any(e => e.Id == id);
        }

        #region privateMethods
        private async Task SendConfirmationEmail(Angajat angajat)
        {
            MimeMessage message = new MimeMessage();
            MailboxAddress from = new MailboxAddress("HRSolution",
            "emimig987@gmail.com");
            message.From.Add(from);
            MailboxAddress to = new MailboxAddress($"{ angajat.Nume + angajat.Prenume}",
            angajat.EmailInstitutional);
            message.To.Add(to);
            message.Subject = "Semnarea noului contract";
            BodyBuilder bodyBuilder = new BodyBuilder();
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string Body = System.IO.File.ReadAllText($"{projectRootPath}/wwwroot/emailTemplate/SemnareActAditional.html");
            bodyBuilder.HtmlBody = Body;
            message.Body = bodyBuilder.ToMessageBody();
            SmtpClient client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _port, true);
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
            try
            {
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
        #endregion
    }
}
