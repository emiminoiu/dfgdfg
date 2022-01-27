using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR.Data;
using HR.Models;
using Microsoft.AspNetCore.Authorization;
using System;
using HR.ViewModels;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Data;
using System.Data.OleDb;
using Microsoft.AspNetCore.Hosting;
using Rotativa.AspNetCore;
using Microsoft.Extensions.Logging;
using GemBox.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Encodings.Web;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using MailKit.Net.Smtp;
using NToastNotify;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AngajatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly IToastNotification _toastNotification;
        private const int numberOfLettersInPassword = 16;
        private const int numberOfNonAphaNumericCharacters = 0;
        private readonly int _port;
        private readonly string _smtpServer;
        private readonly string _username;
        private readonly string _password;
        public AngajatController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment,
            ILogger<AngajatController> logger, UserManager<IdentityUser> userManager,
            IEmailSender emailSender, IToastNotification toastNotification, 
            RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
            _toastNotification = toastNotification;
            _roleManager = roleManager;
            _port = Int32.Parse(configuration["EmailConfiguration:Port"]);
            _smtpServer = configuration["EmailConfiguration:SmtpServer"];
            _username = configuration["EmailConfiguration:Username"];
            _password = configuration["EmailConfiguration:Password"];


        }

        public async Task<IActionResult> Export()
        {
            List<AngajatViewModel> emplist = _context.Angajat.Select(x => new AngajatViewModel
            {
                Nume = x.Nume,
                Prenume = x.Prenume,
                Email = x.Email,
                Sex = x.Sex,
            }).ToList();
            var angajatiMultiselect = new List<string>();
            foreach(var angajat in await _context.Angajat.ToListAsync())
            {
                var angajatMultiselect = $"{angajat.Nume} {angajat.Prenume} - {angajat.EmailInstitutional}";
                angajatiMultiselect.Add(angajatMultiselect);
            }
            ViewData["Angajati"] = angajatiMultiselect;
            return View(new WorkbookModel() { Items = emplist, SelectedFormat = "XLSX" });
        }

        [HttpPost]
        public async Task<ActionResult> ExportToExcel(WorkbookModel model)
        {
            var angajatiSelectati = new List<Angajat>();
            if (model.Angajati != null)
            {
                foreach (var angajat in model.Angajati)
                {
                    string[] interval = angajat.Split('-');
                    var emailInstitutional = interval[1].Trim();
                    var angajatDb = await _context.Angajat.Include(a => a.EvolutieLocDeMunca)
                .Include(a => a.EvolutiePersonala).Include(a => a.Familie).Include(a => a.Salariu).Include(a => a.Sporuri)
                .Include(a => a.ContBancar).FirstOrDefaultAsync(a => a.EmailInstitutional == emailInstitutional);
                    if(angajatDb != null)
                    {
                        angajatiSelectati.Add(angajatDb);
                    }
                }
            }
            else
            {
                angajatiSelectati = await _context.Angajat.Include(a => a.EvolutieLocDeMunca)
                .Include(a => a.EvolutiePersonala).Include(a => a.Familie).Include(a => a.Salariu).Include(a => a.Sporuri)
                .Include(a => a.ContBancar).ToListAsync();
            }
            List<AngajatViewModel> emplist = angajatiSelectati.Select(x => new AngajatViewModel
                {
                    Nume = x.Nume,
                    Prenume = x.Prenume,
                    Email = x.Email,
                    Sex = x.Sex,
                    DataNasterii = x.DataNasterii.ToString(),
                    DomiciuliuLocalitate = x.LocalitateaNasterii,
                    JudetulNasterii = x.JudetulNasterii,
                    Cetatenia = x.Cetatenia,
                    CheieUnica = x.CheieUnica,
                    CNPCI = x.CNPCI,
                    SerieCI = x.SerieCI,
                    TelefonBirou = x.TelefonBirou,
                    TelefonServiciu = x.TelefonServiciu,
                    NumarCI = x.NumarCI,
                    Nationalitatea = x.Nationalitatea,
                    EmailInstitutional = x.EmailInstitutional,
                    EvolutiePersonala = x.EvolutiePersonala,
                    EvolutieLocDeMunca = x.EvolutieLocDeMunca,
                    Familie = x.Familie,
                    Sporuri = x.Sporuri,
                    Salariu = x.Salariu,
                    Functii = x.Functii,
                    ContBancar = x.ContBancar,
                    FunctiaDeConducere = x.Functia,
                    FunctiaDidactica = x.FunctieDidactica
                }).ToList();
            model.Items = emplist;
            SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");

            if (!ModelState.IsValid)
                return View(model);

            var options = GetSaveOptions(model.SelectedFormat);
            var workbook = new ExcelFile();
            var worksheet = workbook.Worksheets.Add("Sheet1");

            var style = worksheet.Rows[0].Style;
            style.Font.Weight = ExcelFont.BoldWeight;
            style.HorizontalAlignment = HorizontalAlignmentStyle.Center;
            worksheet.Columns[0].Style.HorizontalAlignment = HorizontalAlignmentStyle.Center;
            worksheet.Columns[0].SetWidth(50, LengthUnit.Pixel);
            worksheet.Columns[1].SetWidth(150, LengthUnit.Pixel);
            worksheet.Columns[2].SetWidth(150, LengthUnit.Pixel);
            worksheet.Cells["A1"].Value = "Nume";
            worksheet.Cells["B1"].Value = "Prenume";
            worksheet.Cells["C1"].Value = "Email";
            worksheet.Cells["D1"].Value = "Sex";
            worksheet.Cells["E1"].Value = "Data Nasterii";
            worksheet.Cells["F1"].Value = "Localitatea Nasterii";
            worksheet.Cells["G1"].Value = "Judetul Nasterii";
            worksheet.Cells["H1"].Value = "Nationalitatea";
            worksheet.Cells["I1"].Value = "Cetatenia";
            worksheet.Cells["J1"].Value = "Domiciliu Telefon";
            worksheet.Cells["K1"].Value = "Serie CI";
            worksheet.Cells["L1"].Value = "Numar CI";
            worksheet.Cells["M1"].Value = "CNPCI";
            worksheet.Cells["N1"].Value = "Telefon Serviciu";
            worksheet.Cells["O1"].Value = "Telefon Birou";
            worksheet.Cells["P1"].Value = "Functia Didactica";
            worksheet.Cells["Q1"].Value = "Functia de Conducere";
            worksheet.Cells["R1"].Value = "Ultima scoala absolvita";
            worksheet.Cells["S1"].Value = "Data absolvirii scolii";
            worksheet.Cells["T1"].Value = "Localitatea scolii absolvite";
            worksheet.Cells["U1"].Value = "Vechime totala la angajare";
            worksheet.Cells["V1"].Value = "Vechime totala in invatamant";
            worksheet.Cells["W1"].Value = "Data angajarii pe functie didactica";
            worksheet.Cells["X1"].Value = "Data angajarii in invatamant";
            worksheet.Cells["Y1"].Value = "Data angajarii pe functie auxiliara";
            worksheet.Cells["Z1"].Value = "Tip contract";
            worksheet.Cells["A11"].Value = "Vechime totala la angajare";
            worksheet.Cells["B11"].Value = "Vechime totala in invatamant";
            worksheet.Cells["C11"].Value = "Tara Doctorat 1";
            worksheet.Cells["D11"].Value = "Data angajarii pe functie didactica";
            worksheet.Cells["E11"].Value = "Data angajarii in invatamant";
            worksheet.Cells["F11"].Value = "Data angajarii pe functie auxiliara";
            worksheet.Cells["G11"].Value = "Tip contract";
            //worksheet.Cells["H11"].Value = "Numar contract";
            //worksheet.Cells["I11"].Value = "Data start perioada deteriminata";
            //worksheet.Cells["J11"].Value = "Data final perioada deterimanata";
            //worksheet.Cells["K11"].Value = "Serie carnet de munca";
            //worksheet.Cells["L11"].Value = "Calificativ An Curent - 5";
            //worksheet.Cells["M11"].Value = "Calificativ An Curent - 4";
            //worksheet.Cells["N11"].Value = "Calificativ An Curent - 3";
            //worksheet.Cells["O11"].Value = "Calificativ An Curent - 2";
            //worksheet.Cells["P11"].Value = "Calificativ An Curent - 1";
            //worksheet.Cells["Q11"].Value = "Nume Sot";
            //worksheet.Cells["R11"].Value = "Data nasterii sot";
            //worksheet.Cells["S11"].Value = "Profesie Sot";
            //worksheet.Cells["T11"].Value = "Loc de munca sot";
            //worksheet.Cells["U11"].Value = "Salariul Net";
            //worksheet.Cells["V11"].Value = "Salariul Brut";
            //worksheet.Cells["W11"].Value = "Spor Vechime";
            //worksheet.Cells["X11"].Value = "Spor Doctor";
            //worksheet.Cells["Y11"].Value = "Spor Stabilitate";
            //worksheet.Cells["Z11"].Value = "Loc de munca sot";
            //worksheet.Cells["A111"].Value = "Salariul Net";
            //worksheet.Cells["B111"].Value = "Ordin Plata";
            //worksheet.Cells["Z111"].Value = "Numar Ordine";


            for (int r = 1; r <= model.Items.Count; r++)
            {
                var item = model.Items[r - 1];
                worksheet.Cells[r, 0].Value = item.Nume;
                worksheet.Cells[r, 1].Value = item.Prenume;
                worksheet.Cells[r, 2].Value = item.Email;
                worksheet.Cells[r, 3].Value = item.Sex;
                worksheet.Cells[r, 4].Value = item.DataNasterii;
                worksheet.Cells[r, 5].Value = item.DomiciuliuLocalitate;
                worksheet.Cells[r, 6].Value = item.JudetulNasterii;
                worksheet.Cells[r, 7].Value = item.Nationalitatea;
                worksheet.Cells[r, 8].Value = item.Cetatenia;
                worksheet.Cells[r, 9].Value = item.DomiciliuTelefon;
                worksheet.Cells[r, 10].Value = item.SerieCI;
                worksheet.Cells[r, 11].Value = item.NumarCI;
                worksheet.Cells[r, 12].Value = item.CNPCI;
                worksheet.Cells[r, 13].Value = item.TelefonServiciu;
                worksheet.Cells[r, 14].Value = item.TelefonBirou;
                worksheet.Cells[r, 15].Value = item.FunctiaDidactica;
                worksheet.Cells[r, 16].Value = item.FunctiaDeConducere;
                worksheet.Cells[r, 17].Value = item.EvolutiePersonala != null ? item.EvolutiePersonala.UScoalaAbsolvita != null ? item.EvolutiePersonala.UScoalaAbsolvita : "necompletat" : "necompletat";
                worksheet.Cells[r, 18].Value = item.EvolutiePersonala != null ? item.EvolutiePersonala.DataAbsolviriiScolii != null ? item.EvolutiePersonala.DataAbsolviriiScolii : new DateTime() : new DateTime(); 
                worksheet.Cells[r, 19].Value = item.EvolutiePersonala != null ? item.EvolutiePersonala.LocalitateaUScoliAbsolvite != null ? item.EvolutiePersonala.LocalitateaUScoliAbsolvite : "necompletat" : "necompletat"; 
                worksheet.Cells[r, 20].Value = item.EvolutiePersonala != null ? item.EvolutiePersonala.VechimeTotalaAngajare != null ? item.EvolutiePersonala.VechimeTotalaAngajare : 0:0;
                worksheet.Cells[r, 21].Value = item.EvolutiePersonala != null ? item.EvolutiePersonala.VechimeTotalaInvatamant : 0;
                worksheet.Cells[r, 22].Value = item.EvolutiePersonala != null ? item.EvolutieLocDeMunca.DataAngajariiFctDidactica: new DateTime();
                worksheet.Cells[r, 23].Value = item.EvolutiePersonala != null ? item.EvolutieLocDeMunca.DataAngajariiInvatamant: new DateTime();
                worksheet.Cells[r, 24].Value = item.EvolutiePersonala != null ? item.EvolutieLocDeMunca.DataAngajariiFctAuxiliara : new DateTime();
                worksheet.Cells[r, 25].Value = item.EvolutiePersonala != null ? item.EvolutieLocDeMunca.TipContract : "necompletat";
                //worksheet.Cells[r, 11].Value = item.SerieCI;
                //worksheet.Cells[r, 12].Value = item.NumarCI;
                //worksheet.Cells[r, 13].Value = item.CNPCI;
                //worksheet.Cells[r, 14].Value = item.TelefonServiciu;
                //worksheet.Cells[r, 15].Value = item.TelefonBirou;
                //worksheet.Cells[r, 16].Value = item.EmailInstitutional;
            }
            return File(GetBytes(workbook, options), options.ContentType, "RaportAngajati." + model.SelectedFormat.ToLowerInvariant());
        }

        private static SaveOptions GetSaveOptions(string format)
        {
            switch (format.ToUpperInvariant())
            {
                case "XLSX":
                    return SaveOptions.XlsxDefault;
                case "XLS":
                    return SaveOptions.XlsDefault;
                case "ODS":
                    return SaveOptions.OdsDefault;
                case "CSV":
                    return SaveOptions.CsvDefault;
                case "HTML":
                    return SaveOptions.HtmlDefault;
                case "PDF":
                    return SaveOptions.PdfDefault;
                case "XPS":
                    return SaveOptions.XpsDefault;
                case "BMP":
                    return new ImageSaveOptions() { Format = ImageSaveFormat.Bmp };
                case "GIF":
                    return new ImageSaveOptions() { Format = ImageSaveFormat.Gif };
                case "JPG":
                    return new ImageSaveOptions() { Format = ImageSaveFormat.Jpeg };
                case "PNG":
                    return new ImageSaveOptions() { Format = ImageSaveFormat.Png };
                case "TIF":
                    return new ImageSaveOptions() { Format = ImageSaveFormat.Tiff };
                case "WMP":
                    return new ImageSaveOptions() { Format = ImageSaveFormat.Wmp };
                default:
                    throw new NotSupportedException("Format '" + format + "' is not supported.");
            }
        }

        private static byte[] GetBytes(ExcelFile file, SaveOptions options)
        {
            using (var stream = new MemoryStream())
            {
                file.Save(stream, options);
                return stream.ToArray();
            }
        }

        // GET: Angajats
        public async Task<IActionResult> Index(string searchTerm)
        {
            List<Angajat> angajati = new List<Angajat>();
            if (searchTerm != null)
            {
                angajati = await _context.Angajat.Where(a => a.Prenume.Contains(searchTerm)
                || a.Email.Contains(searchTerm) || a.Nume.Contains(searchTerm)).ToListAsync();
            }
            else
            {
                angajati = await _context.Angajat.OrderByDescending(a => a.Id).ToListAsync();
            }

            return View(angajati);
        }

        public async Task<IActionResult> ImportFromExcel()
        {
            return View();
        }

        //https://github.com/kannadasbe/ImportDataFromExcel/blob/master/AspDotNetMVCDemo/Views/Import/Index.cshtml

        public async Task<ActionResult> ImportExcel(IFormFile postedFile)
        {

            if (postedFile != null)
            {
                try
                {
                    string fileExtension = Path.GetExtension(postedFile.FileName);

                    //Validate uploaded file and return error.
                    if (fileExtension != ".xls" && fileExtension != ".xlsx")
                    {
                        ViewBag.Message = "Please select the excel file with .xls or .xlsx extension";
                        return View();
                    }

                    string folderPath = _hostingEnvironment.ContentRootPath;
                    //Check Directory exists else create one
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    //Save file to folder
                    var filePath = folderPath + Path.GetFileName(postedFile.FileName);
                    //Get file extension

                    string excelConString = "";

                    //Get connection string using extension 
                    switch (fileExtension)
                    {
                        //If uploaded file is Excel 1997-2007.
                        case ".xls":
                            excelConString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties='Excel 8.0;HDR=YES";
                            break;
                        //If uploaded file is Excel 2007 and above
                        case ".xlsx":
                            excelConString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=Excel 8.0;HDR=YES";
                            break;
                    }

                    //Read data from first sheet of excel into datatable
                    DataTable dt = new DataTable();
                    excelConString = string.Format("Provider = Microsoft.ACE.OLEDB.12.0; Data Source = {0}; Extended Properties = Excel 8.0; HDR = YES", filePath);

                    using (OleDbConnection excelOledbConnection = new OleDbConnection(excelConString))
                    {
                        using (OleDbCommand excelDbCommand = new OleDbCommand())
                        {
                            using (OleDbDataAdapter excelDataAdapter = new OleDbDataAdapter())
                            {
                                excelDbCommand.Connection = excelOledbConnection;

                                excelOledbConnection.Open();
                                //Get schema from excel sheet
                                DataTable excelSchema = GetSchemaFromExcel(excelOledbConnection);
                                //Get sheet name
                                string sheetName = excelSchema.Rows[0]["TABLE_NAME"].ToString();
                                excelOledbConnection.Close();

                                //Read Data from First Sheet.
                                excelOledbConnection.Open();
                                excelDbCommand.CommandText = "SELECT * From [" + sheetName + "]";
                                excelDataAdapter.SelectCommand = excelDbCommand;
                                //Fill datatable from adapter
                                excelDataAdapter.Fill(dt);
                                excelOledbConnection.Close();
                            }
                        }
                    }

                    //Insert records to Employee table.

                    //Loop through datatable and add employee data to employee table. 
                    foreach (DataRow row in dt.Rows)
                    {
                        _context.Angajat.Add(GetEmployeeFromExcelRow(row));
                    }
                    _context.SaveChanges();
                    ViewBag.Message = "Data Imported Successfully.";
                }
                catch (Exception ex)
                {
                    ViewBag.Message = ex.Message;
                }
            }
            else
            {
                ViewBag.Message = "Please select the file first to upload.";
            }
            return RedirectToAction("Index");
        }


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

        // GET: Angajats/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var angajat = await _context.Angajat
                .FirstOrDefaultAsync(m => m.Id == id);
            if (angajat == null)
            {
                return NotFound();
            }

            return View(angajat);
        }

        // GET: Angajats/Create
        public IActionResult Create()
        {
            ViewData["FunctiiDidactice"] = GetFunctiiDidacticeList();
            ViewData["FunctiiConducere"] = GetFunctiiConducereList();
            List<string> genuri = new List<string>();
            genuri.Add("Masculin");
            genuri.Add("Feminin");
            ViewData["GenDropdown"] = genuri;
            List<int> gradFunctiiConducere = new List<int>();
            gradFunctiiConducere.Add(1);
            gradFunctiiConducere.Add(2);
            ViewData["GradFunctiiConducere"] = gradFunctiiConducere;
            return View();
        }



        // POST: Angajats/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nume,Prenume,Sex,DataNasterii,LocalitateaNasterii," +
            "JudetulNasterii,Nationalitatea,Cetatenia,DomiciuLocalitate,DomiciliuStrada,DomiciliuJudet,DomiciliuTelefon," +
            "SerieCI,NumarCI,DataEliberariiCI,EliberataDeCI,CNPCI,DataExpirariiCI,TelefonServiciu,TelefonBirou," +
            "Email,EmailInstitutional, Functia, FunctieDidactica, GradulFunctieiDeConducere")] Angajat angajat, [FromForm] bool functieConducere)
        {
            if (ModelState.IsValid)
            {
                if (angajat.Functia != null)
                {
                    angajat.FunctieDeConducere = true;
                }
                else
                {
                    angajat.FunctieDeConducere = false;
                }
                angajat.CreatedAt = DateTime.Now;
                angajat.NumarZileLibereRamase = 42;
                _context.Add(angajat);
                await _context.SaveChangesAsync();
                await GenerateAccount(angajat);
                await _context.SaveChangesAsync();
                return RedirectToAction("Create", "Familie", new { @id = angajat.Id });
            }
            
            ViewData["FunctiiDidactice"] = GetFunctiiDidacticeList();
            ViewData["FunctiiConducere"] = GetFunctiiConducereList();
            List<string> genuri = new List<string>();
            genuri.Add("Masculin");
            genuri.Add("Feminin");
            ViewData["GenDropdown"] = genuri;
            List<int> gradFunctiiConducere = new List<int>();
            gradFunctiiConducere.Add(1);
            gradFunctiiConducere.Add(2);
            ViewData["GradFunctiiConducere"] = gradFunctiiConducere;
            return View(angajat);
        }





        public async Task<IActionResult> GeneratePdf(int? id)
        {
            EmployeePdfViewModel employeePdf = new EmployeePdfViewModel();
            if (id == null)
            {
                return NotFound();
            }

            var angajat = await _context.Angajat.Include(a => a.ContBancar)
                .Include(a => a.Salariu).Include(a => a.Familie)
                .Include(a => a.EvolutiePersonala).Include(a => a.EvolutieLocDeMunca)
                .Include(a => a.Functii).Include(a => a.Sporuri)
            .FirstOrDefaultAsync(a => a.Id == id);

            if (angajat == null)
            {
                return NotFound();
            }
            employeePdf = GetEmployeeData(angajat);
            return new ViewAsPdf("GeneratePdf", employeePdf);
            //return View(employeePdf);
        }

        // GET: Angajats/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var angajat = await _context.Angajat.Include(angajat => angajat.ContBancar)
          .Include(angajat => angajat.Salariu).FirstOrDefaultAsync(a => a.Id == id);
          
            ViewData["FunctiiDidactice"] = GetFunctiiDidacticeList();
            ViewData["FunctiiConducere"] = GetFunctiiConducereList();
            List<int> gradFunctiiConducere = new List<int>();
            gradFunctiiConducere.Add(1);
            gradFunctiiConducere.Add(2);
            ViewData["GradFunctiiConducere"] = gradFunctiiConducere;
            List<string> genuri = new List<string>();
            genuri.Add("Masculin");
            genuri.Add("Feminin");
            ViewData["GenDropdown"] = genuri;


            if (angajat == null)
            {
                return NotFound();
            }
            return View(angajat);
        }


        // POST: Angajats/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nume,Prenume,Sex,DataNasterii,LocalitateaNasterii,JudetulNasterii,Nationalitatea," +
            "Cetatenia,DomiciuLocalitate,DomiciliuStrada,DomiciliuJudet,DomiciliuTelefon,SerieCI,NumarCI,DataEliberariiCI,EliberataDeCI," +
            "CNPCI,DataExpirariiCI,TelefonServiciu,TelefonBirou,Email,EmailInstitutional, Functia, FunctieDidactica,FunctieDeConducere,GradulFunctieiDeConducere")] Angajat angajat)
        {

            if (ModelState.IsValid)
            {
                if (angajat.FunctieDidactica != null)
                {
                    angajat.FunctieDeConducere = false;
                  
                }
                else
                {
                    angajat.FunctieDeConducere = true;
                   
                }
                if (id != angajat.Id)
                {
                    return NotFound();
                }
                var angajatExistent = await _context.Angajat.FirstOrDefaultAsync(a => a.Id == angajat.Id);
                var local = _context.Set<Angajat>()
             .Local
             .FirstOrDefault(entry => entry.Id.Equals(angajatExistent.Id));
                _context.Entry(local).State = EntityState.Detached;
                _context.Entry(angajatExistent).State = EntityState.Detached;
                angajat.CheieUnica = angajatExistent.CheieUnica;
                angajat.CreatedAt = angajatExistent.CreatedAt;
                _context.Update(angajat);
                await _context.SaveChangesAsync();
                if(angajat.FunctieDeConducere == false)
                {
                    var evolutiePersonala = await _context.EvolutiePersonala.FirstOrDefaultAsync(ev => ev.AngajatId == angajat.Id);
                    var salariu = await _context.Salariu.FirstOrDefaultAsync(s => s.AngajatId == angajat.Id);
                    if (evolutiePersonala != null && salariu != null)
                    {
                        var vechime = evolutiePersonala.VechimeTotalaInvatamant;
                        salariu.SalariuNet = await CalculareSalariuNetPentruFunctiiDidactice(angajat, vechime);
                    }
                }
                else
                {
                    var salariu = await _context.Salariu.FirstOrDefaultAsync(s => s.AngajatId == angajat.Id);
                    if (salariu != null)
                    {
                        salariu.SalariuNet = await CalculareSalariuNetPentruFunctiiConducere(angajat, angajat.GradulFunctieiDeConducere);
                    }
                }
                await _context.SaveChangesAsync();
                _toastNotification.AddSuccessToastMessage("Datele au fost modificate cu success!");
            }
            else
            {
                _toastNotification.AddErrorToastMessage("Ceva nu a mers bine!");
            }

            
            ViewData["FunctiiDidactice"] = GetFunctiiDidacticeList();
            ViewData["FunctiiConducere"] = GetFunctiiConducereList();
            List<int> gradFunctiiConducere = new List<int>();
            gradFunctiiConducere.Add(1);
            gradFunctiiConducere.Add(2);
            ViewData["GradFunctiiConducere"] = gradFunctiiConducere;

            List<string> genuri = new List<string>();
            genuri.Add("Masculin");
            genuri.Add("Feminin");
            ViewData["GenDropdown"] = genuri;

            return View("Edit", angajat);
        }

        // GET: Angajats/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var angajat = await _context.Angajat
                .FirstOrDefaultAsync(m => m.Id == id);
            if (angajat == null)
            {
                return NotFound();
            }

            return View(angajat);
        }

        // POST: Angajats/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var angajat = await _context.Angajat.FindAsync(id);
            _context.Angajat.Remove(angajat);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AngajatExists(int id)
        {
            return _context.Angajat.Any(e => e.Id == id);
        }

        #region PrivateMethods
        private static EmployeePdfViewModel GetEmployeeData(Angajat angajat)
        {
            EmployeePdfViewModel employeePdf = new EmployeePdfViewModel();
            employeePdf.Nume = angajat.Nume;
            employeePdf.Prenume = angajat.Prenume;
            employeePdf.SerieCI = angajat.SerieCI;
            employeePdf.Email = angajat.Email;
            employeePdf.Sex = angajat.Sex;
            employeePdf.DataNasterii = angajat.DataNasterii;
            employeePdf.DomiciliuJudet = angajat.DomiciliuJudet;
            employeePdf.DomiciliuStrada = angajat.DomiciliuStrada;
            employeePdf.DomiciliuTelefon = angajat.DomiciliuTelefon;
            employeePdf.Nationalitatea = angajat.Nationalitatea;
            employeePdf.DomiciuLocalitate = angajat.DomiciuLocalitate;
            employeePdf.NumarCI = angajat.NumarCI;
            employeePdf.Cetatenia = angajat.Cetatenia;
            employeePdf.CNPCI = angajat.CNPCI;
            employeePdf.TelefonBirou = angajat.TelefonBirou;
            employeePdf.TelefonServiciu = angajat.TelefonServiciu;
            employeePdf.Familie = angajat.Familie;
            employeePdf.EvolutiePersonala = angajat.EvolutiePersonala;
            employeePdf.EvolutieLocDeMunca = angajat.EvolutieLocDeMunca;
            employeePdf.Salariu = angajat.Salariu;
            employeePdf.Sporuri = angajat.Sporuri;
            employeePdf.Functii = angajat.Functii;
            employeePdf.ContBancar = angajat.ContBancar;
            return employeePdf;
        }

        public static string GenerateRandomPassword(PasswordOptions opts = null)
        {
            if (opts == null) opts = new PasswordOptions()
            {
                RequiredLength = 8,
                RequiredUniqueChars = 4,
                RequireDigit = true,
                RequireLowercase = true,
                RequireNonAlphanumeric = true,
                RequireUppercase = true
            };

            string[] randomChars = new[] {
            "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
            "abcdefghijkmnopqrstuvwxyz",    // lowercase
            "0123456789",                   // digits
            "!@$?_-"                        // non-alphanumeric
        };

            Random rand = new Random(Environment.TickCount);
            List<char> chars = new List<char>();

            if (opts.RequireUppercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[0][rand.Next(0, randomChars[0].Length)]);

            if (opts.RequireLowercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[1][rand.Next(0, randomChars[1].Length)]);

            if (opts.RequireDigit)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[2][rand.Next(0, randomChars[2].Length)]);

            if (opts.RequireNonAlphanumeric)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[3][rand.Next(0, randomChars[3].Length)]);

            for (int i = chars.Count; i < opts.RequiredLength
                || chars.Distinct().Count() < opts.RequiredUniqueChars; i++)
            {
                string rcs = randomChars[rand.Next(0, randomChars.Length)];
                chars.Insert(rand.Next(0, chars.Count),
                    rcs[rand.Next(0, rcs.Length)]);
            }

            return new string(chars.ToArray());
        }

        private async Task<IActionResult> GenerateAccount(Angajat angajat)
        {
            var user = new UtilizatorulMeu { UserName = angajat.EmailInstitutional, Email = angajat.EmailInstitutional };
            string password = GenerateRandomPassword(null);
            var identityResult = await _userManager.CreateAsync(user, password);
            user.EmailConfirmed = true;
            user.AngajatId = angajat.Id;
            bool roleExist = await _roleManager.RoleExistsAsync("Cadru Didactic");
            if (!roleExist)
            {
                var role = new IdentityRole();
                role.Name = "Cadru Didactic";
                await _roleManager.CreateAsync(role);
            }
            await _userManager.AddToRoleAsync(user, "Cadru Didactic");
            if (identityResult.Succeeded)
            {
                await SendConfirmationEmail(user, angajat, password);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code = code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(angajat.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                return Ok();
            }
            return BadRequest();
        }
        private async Task SendConfirmationEmail(UtilizatorulMeu utilizator, Angajat angajat, string password)
        {
            MimeMessage message = new MimeMessage();
            MailboxAddress from = new MailboxAddress("HRSolution",
            "emimig987@gmail.com");
            message.From.Add(from);
            MailboxAddress to = new MailboxAddress($"{ angajat.Nume + angajat.Prenume}",
            angajat.EmailInstitutional);
            message.To.Add(to);
            message.Subject = "Detalii Cont HR(Solution)";
            BodyBuilder bodyBuilder = new BodyBuilder();
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string Body = System.IO.File.ReadAllText($"{projectRootPath}/wwwroot/emailTemplate/DetaliiAccount.html");
            Body = Body.Replace("#nume#", $"{angajat.Nume + " " + angajat.Prenume}");
            Body = Body.Replace("#username#", $"{utilizator.UserName}");
            Body = Body.Replace("#parola#", $"{password}");
            bodyBuilder.HtmlBody = Body;
            message.Body = bodyBuilder.ToMessageBody();
            SmtpClient client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _port, true);
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
            try
            {
                System.Threading.Thread.Sleep(1500);
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

        private async Task<decimal> CalculareSalariuNetPentruFunctiiDidactice(Angajat angajat, int vechimea)
        {
            var functiiDidacticePosibile = await _context.GradeDidactice.Where(g => g.Nume == angajat.FunctieDidactica).ToListAsync();
            decimal salariuNet = 0;
            foreach (var functie in functiiDidacticePosibile)
            {
                string[] interval = functie.IntervalAni.Split('-');
                var startIntervalAni = Int32.Parse(interval[0]);
                var finalIntervalAni = Int32.Parse(interval[1]);
                if (vechimea >= startIntervalAni && vechimea <= finalIntervalAni)
                {
                    salariuNet = functie.SalariuBaza;
                    angajat.GradDidacticId = functie.Id;
                }
            }
            return salariuNet;
        }

        private async Task<decimal> CalculareSalariuNetPentruFunctiiConducere(Angajat angajat, int? gradul)
        {
            var functiiConducere = await _context.FunctiiConducere.FirstOrDefaultAsync(g => g.Nume == angajat.Functia && g.Grad == gradul);
            angajat.FunctieConducereId = functiiConducere.Id;
            decimal salariuNet = functiiConducere.SalariuBaza;
            return salariuNet;
        }

        public List<string> GetFunctiiDidacticeList()
        {
            List<string> functiiDidactice = new List<string>();
            functiiDidactice.Add("Asistent Universitar");
            functiiDidactice.Add("Sef lucrări (lector universitar)");
            functiiDidactice.Add("Conferentiar universitar");
            functiiDidactice.Add("Profesor universitar");
            return functiiDidactice;
        }

     

        public List<string> GetFunctiiConducereList()
        {
            List<string> functiiConducere = new List<string>();
            functiiConducere.Add("Rector");
            functiiConducere.Add("Prorector");
            functiiConducere.Add("Director general administrativ al universitatii");
            functiiConducere.Add("Decan");
            functiiConducere.Add("Prodecan");
            functiiConducere.Add("Director de departament");
            functiiConducere.Add("Director general adjunct administrativ al universitatii");
            return functiiConducere;
        }
        #endregion
    }
}
