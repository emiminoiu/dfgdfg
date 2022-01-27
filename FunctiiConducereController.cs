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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class FunctiiConducereController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly int _port;
        private readonly string _smtpServer;
        private readonly string _username;
        private readonly string _password;
        public FunctiiConducereController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment, IConfiguration configuration)
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
            var functiiConducere = await _context.FunctiiConducere.OrderByDescending(f => f.Id).ToListAsync();
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
        public async Task<IActionResult> Create([Bind("Nume, Grad, SalariuBaza")] FunctieConducere functieConducere)
        {
            if (ModelState.IsValid)
            {
                _context.FunctiiConducere.Add(functieConducere);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(functieConducere);
        }

        // GET: Sporuris/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var functieConducere = await _context.FunctiiConducere.FirstOrDefaultAsync(s => s.Id == id);

            return View(functieConducere);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Json(new { success = true, data = await _context.FunctiiConducere.OrderByDescending(f => f.Id).ToListAsync() });
        }

        // POST: Sporuris/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id, Nume, Grad, SalariuBaza")]FunctieConducere functieConducere)
        {
            if (id != functieConducere.Id)
            {
                return NotFound();
            }
            var functieInitiala = await _context.FunctiiConducere.FirstOrDefaultAsync(g => g.Id == id);
            var local = _context.Set<FunctieConducere>()
           .Local
           .FirstOrDefault(entry => entry.Id.Equals(functieInitiala.Id));
            _context.Entry(local).State = EntityState.Detached;
            _context.Entry(functieInitiala).State = EntityState.Detached;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(functieConducere);
                    await _context.SaveChangesAsync();
                    if(functieInitiala.SalariuBaza != functieConducere.SalariuBaza)
                    {
                        var angajati = await _context.Angajat.Where(a => a.FunctieConducereId == functieConducere.Id).ToListAsync();
                        foreach (var angajat in angajati)
                        {
                            await SendConfirmationEmail(angajat);
                        }
                    }
                  
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FunctiaDeConducereExista(functieConducere.Id))
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
            var functieConducere = await _context.FunctiiConducere.FindAsync(id);
            _context.FunctiiConducere.Remove(functieConducere);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Delete successful" });
        }



        private bool FunctiaDeConducereExista(int id)
        {
            return _context.FunctiiConducere.Any(e => e.Id == id);
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
