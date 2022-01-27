using HR.Data;
using HR.Models;
using HR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class DocumenteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public DocumenteController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Sporuris
        public async Task<IActionResult> Index(int Id)
        {
            var angajat = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == Id);
            ViewBag.NumeAngajat = $"{angajat.Nume} {angajat.Prenume}";
            var documente = await _context.Documente.Where(d => d.AngajatId == Id).OrderByDescending(a => a.Id).ToListAsync();
            var documenteModel = new DocumenteViewModel();
            documenteModel.Documente = documente;
            documenteModel.AngajatId = Id;
            return View(documenteModel);
        }


        // GET: Sporuris/Create 
        public IActionResult Create(int id)
        {
            var document = new Document();
            document.AngajatId = id;
            return View(document);
        }

        //// POST: Sporuris/Create
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nume, AngajatId")] Document document, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        file.CopyTo(ms);
                        document.Tip = Path.GetExtension(file.FileName).Replace(".", "");
                        document.Continut = ms.ToArray();
                    }
                }
                _context.Documente.Add(document);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { id = document.AngajatId});
            }
            return View(document);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocument(int Id)
        {
            var document = await _context.Documente.FirstOrDefaultAsync(d => d.Id == Id);
            return Json(new { success = true, data = document });
        }
      
        // GET: Sporuris/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var document = await _context.Documente.FindAsync(id);
            _context.Documente.Remove(document);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Delete successful" });
        }

    }
}
