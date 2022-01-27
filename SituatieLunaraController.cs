using HR.Data;
using HR.Models;
using HR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using GemBox.Spreadsheet;
using System.IO;
using System;

namespace HR.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class SituatieLunaraController : Controller
    {
        #region privateMembers
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        #endregion
        #region Constructor
        public SituatieLunaraController(ApplicationDbContext _context, IToastNotification _toastNotification)
        {
            this._context = _context;
            this._toastNotification = _toastNotification;
        }
        #endregion

        #region publicMethods
        public async Task<IActionResult> SituatieLunara()
        {
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
            var angajatiMultiselect = new List<string>();

            foreach (var angajat in await _context.Angajat.ToListAsync())
            {
                var angajatMultiselect = $"{angajat.Nume} {angajat.Prenume} - {angajat.EmailInstitutional}";
                angajatiMultiselect.Add(angajatMultiselect);
            }

            ViewData["Angajati"] = angajatiMultiselect;

            return View();
        }

        public async Task<IActionResult> SalveazaSituatie(SituatieLunaraExportViewModel model, bool exportAngajati)
        {
            var angajatiSelectati = new List<Angajat>();
            if (model.AngajatiSelectati)
            {
                if (model.Angajati != null)
                {
                    foreach (var angajat in model.Angajati)
                    {
                        string[] interval = angajat.Split('-');
                        var emailInstitutional = interval[1].Trim();
                        var angajatDb = await _context.Angajat.Include(a => a.EvolutieLocDeMunca)
                    .Include(a => a.EvolutiePersonala).Include(a => a.Familie).Include(a => a.Salariu).Include(a => a.Sporuri)
                    .Include(a => a.ContBancar).FirstOrDefaultAsync(a => a.EmailInstitutional == emailInstitutional);
                        if (angajatDb != null)
                        {
                            angajatiSelectati.Add(angajatDb);
                        }
                    }
                    foreach (var angajat in angajatiSelectati)
                    {
                        SituatieLunara situatie = new SituatieLunara();
                        situatie.AngajatId = angajat.Id;
                        situatie.Luna = model.Luna;
                        situatie.An = model.An;
                        var salariu = await _context.Salariu.FirstOrDefaultAsync(s => s.AngajatId == angajat.Id);
                        if (salariu != null)
                        {
                            situatie.SalariuBrut = salariu.SalariuBrut != null ? salariu.SalariuBrut : 0;
                            situatie.SalariuNet = salariu.SalariuNet != null ? salariu.SalariuNet : 0;
                            _context.SituatiiLunare.Add(situatie);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await SaveSituation(model);
                }
            }

            else
            {
                await SaveSituation(model);
            }

            if (model.AlteFormate)
            {
                var totiAngajatii = await _context.Angajat.Include(a => a.Salariu).ToListAsync();
                var angajatiExport = new List<Angajat>();
                angajatiExport = angajatiSelectati.Count > 0 ? angajatiSelectati : totiAngajatii;
                //List<AngajatExportSituatieLunaraViewModel> exportItems = new List<AngajatExportSituatieLunaraViewModel>();
                //exportItems = angajatiExport.Select(x => new AngajatExportSituatieLunaraViewModel
                //{
                //    Nume = x.Nume,
                //    Prenume = x.Prenume,
                //    Luna = model.Luna,
                //    Anul = model.An,
                //    SalariuBrut = x.Salariu.SalariuBrut != null ? x.Salariu.SalariuBrut : 0,
                //    SalariuNet = x.Salariu.SalariuNet != null  ? x.Salariu.SalariuNet : 0
                //}).ToList();
                if (model.SelectedFormat != null)
                {
                    SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");

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
                    worksheet.Cells["C1"].Value = "Salariu Net";
                    worksheet.Cells["D1"].Value = "Salariu Brut";
                    worksheet.Cells["E1"].Value = "Luna";
                    worksheet.Cells["F1"].Value = "Anul";

                    for (int r = 1; r <= angajatiExport.Count; r++)
                    {
                        var item = angajatiExport[r - 1];
                        worksheet.Cells[r, 0].Value = item.Nume;
                        worksheet.Cells[r, 1].Value = item.Prenume;
                        worksheet.Cells[r, 2].Value = item.Salariu.SalariuNet != null ? item.Salariu.SalariuNet : 0;
                        worksheet.Cells[r, 3].Value = item.Salariu.SalariuBrut != null ? item.Salariu.SalariuBrut : 0;
                        worksheet.Cells[r, 4].Value = model.Luna;
                        worksheet.Cells[r, 5].Value = model.An;

                    }
                    return File(GetBytes(workbook, options), options.ContentType, "SituatiaSalariala." + model.SelectedFormat.ToLowerInvariant());
                }
            }
            _toastNotification.AddSuccessToastMessage("Situatia a fost salvata cu succes!");
            return RedirectToAction("SituatieLunara");
        }
        #endregion

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

        public async Task SaveSituation(SituatieLunaraExportViewModel model)
        {
            foreach (var angajat in await _context.Angajat.Include(a => a.Salariu).ToListAsync())
            {
                SituatieLunara situatie = new SituatieLunara();
                situatie.AngajatId = angajat.Id;
                situatie.Luna = model.Luna;
                situatie.An = model.An;
                var salariu = await _context.Salariu.FirstOrDefaultAsync(s => s.AngajatId == angajat.Id);
                if (salariu != null)
                {
                    situatie.SalariuBrut = salariu.SalariuBrut != null ? salariu.SalariuBrut : 0;
                    situatie.SalariuNet = salariu.SalariuNet != null ? salariu.SalariuNet : 0;
                    _context.SituatiiLunare.Add(situatie);
                }
            }
            await _context.SaveChangesAsync();
        }
    }

}