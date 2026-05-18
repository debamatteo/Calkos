
//TOGLIERE IL REM .MODELLO Contiene solo le funzioni per l'importazione excel

//using Calkos.web.Helpers;
//using Calkos.web.Models.DTO;
//using Calkos.web.Models.ViewModels.Prospetti;
//using Calkos.web.Services.Export;
//using Calkos.web.Services.Prospetti;
//using CalkosManager.Application.Interfaces;
//using CalkosManager.Application.Services;
//using CalkosManager.Domain.Entities;
//using CalkosManager.Domain.Interfaces.Repositories;
//using CalkosManager.Domain.Models.Importazione;
//using CalkosManager.Infrastructure.Repositories;
//using CalkosManager.Infrastructure.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.AspNetCore.Mvc.ViewEngines;
//using Microsoft.AspNetCore.Mvc.ViewFeatures;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Newtonsoft.Json;
//using NuGet.Protocol.Core.Types;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Globalization;

////Services\Export\ExcelExportService.cs


//namespace Calkos.web.Areas.Admin.Controllers
//{
//    [Area("Admin")]
//    [Authorize]
//    // Antiforgery 05/04/2026: Aggiunto per centralizzare la validazione Antiforgery su tutte le POST del controller
//    [AutoValidateAntiforgeryToken]
//    public class ProspettiDeAngeliController : Controller
//    {
//        /*
//         * Questo controller gestisce TUTTO ciò che riguarda DeAngeli:
//         * - Importazione file Excel DeAngeli
//         * - Conversione righe Excel → ImportDeAngeli
//         * - Salvataggio in FileImportato + ProspettoDeAngeli
//         * - Visualizzazione lista ordini DeAngeli
//         * 
//         * È la versione SPECIALIZZATA del vecchio ProspettiController,
//         * ma dedicata SOLO al Mandatario DeAngeli.
//         */

//        // Il servizio che contiene tutta la pipeline di importazione.
//        // Il controller NON fa logica: delega tutto al service.
//        private readonly ImportazioneDeAngeliService _ImportazioneDeAngeliService;

//        /*
//         * Il repository DeAngeli.
//         * 
//         * Il controller NON deve conoscere come il repository funziona internamente.
//         * Usa SOLO l’interfaccia IProspettoDeAngeliRepository.
//         * 
//         * In Program.cs abbiamo:
//         * services.AddScoped<IProspettoDeAngeliRepository, ProspettoDeAngeliRepository>();
//         * 
//         * Questo significa:
//         * “Quando qualcuno chiede IProspettoDeAngeliRepository, dagli ProspettoDeAngeliRepository”.
//         */
//        private readonly IProspettoDeAngeliRepository _prospettoRepository;
//        private readonly IAgenteRepository _agenteRepository; // serve per lookup agenti
//        private readonly IFileBackupService _fileBackupService; // serve per backup file DeAngeli
//        private readonly IClienteRepository _clienteRepository; // serve per lookup clienti
//        private readonly IMaterialeRepository _materialeRepository; // serve per lookup materiale
//        private readonly ITipoPagamentoRepository _tipoPagamentoRepository; // serve per lookup tipo pagamento
//        private readonly IUnitaMisuraRepository _unitaMisuraRepository; // serve per lookup unità di misura
//        private readonly ProspettoConfigService _configService;

//        private readonly ExcelExportService _excelExportService;
//        private readonly PdfExportService _pdfExportService;
//        private readonly IConfiguration _config;//legge i parametri da appsettings.json
//        private readonly MandatarioService _mandatarioService;

//        public ProspettiDeAngeliController(
//            ImportazioneDeAngeliService ImportazioneDeAngeliService,
//            IProspettoDeAngeliRepository prospettoRepository,
//            IAgenteRepository agenteRepository,
//            IClienteRepository clienteRepository,
//            IMaterialeRepository materialeRepository,
//            ITipoPagamentoRepository tipoPagamentoRepository,
//            IUnitaMisuraRepository unitaMisuraRepository,
//            IFileBackupService fileBackupService,
//            ProspettoConfigService configService,
//            ExcelExportService excelExportService,
//             PdfExportService pdfExportService,
//             IConfiguration config,
//             MandatarioService mandatarioService)//legge i parametri da appsettings.json
//        {
//            _ImportazioneDeAngeliService = ImportazioneDeAngeliService;
//            _prospettoRepository = prospettoRepository;
//            _agenteRepository = agenteRepository;
//            _clienteRepository = clienteRepository;
//            _materialeRepository = materialeRepository;
//            _tipoPagamentoRepository = tipoPagamentoRepository;
//            _unitaMisuraRepository = unitaMisuraRepository;
//            _fileBackupService = fileBackupService;
//            _configService = configService;   // iniettare il servizio di confiugurazione per la lettura delle colonne da visualizzare in lista ordini
//            _excelExportService = excelExportService;
//            _pdfExportService = pdfExportService;
//            _config = config;//legge i parametri da appsettings.json
//            _mandatarioService = mandatarioService;
//        }

//        // ============================================================
//        // 2) GET: PAGINA DI IMPORTAZIONE DEANGELI
//        // ============================================================

//        //[HttpGet]
//        //[Authorize(Roles = "Admin")]
//        //public IActionResult Importa(int idMandatario)
//        //{

//        //    // Salvo IdMandatario in sessione per renderlo stabile
//        //    // per tutto il flusso di importazione DEANGELI
//        //    HttpContext.Session.SetInt32("IdMandatario", idMandatario);

//        //    // Mantengo anche il ViewBag per la view, se serve
//        //    ViewBag.IdMandatario = idMandatario;

//        //    return View();
//        //}
//        [HttpGet]
//        [Authorize(Roles = "Admin")]
//        public IActionResult Importa(int idMandatario)
//        {
//            // Salvo IdMandatario in sessione
//            HttpContext.Session.SetInt32("IdMandatario", idMandatario);

//            // Recupero il tipo pagamento del mandatario
//            int tipoPagamento = _mandatarioService.GetTipoPagamento(idMandatario);

//            // Lo salvo in sessione per tutto il flusso DEANGELI
//            HttpContext.Session.SetInt32("TipoPagamentoMandatario", tipoPagamento);

//            // Mantengo anche il ViewBag se serve
//            ViewBag.IdMandatario = idMandatario;
//            ViewBag.TipoPagamento = tipoPagamento;

//            return View();
//        }

//        // ============================================================
//        // 3) POST: ESECUZIONE IMPORTAZIONE DEANGELI
//        // ============================================================
//        /*
//         * Questo metodo:
//         * 1. Legge il file Excel
//         * 2. Converte ogni riga in ImportDeAngeli
//         * 3. Crea un FileImportato
//         * 4. Avvia la pipeline di importazione tramite ImportazioneDeAngeliService
//         * 5. Mostra una modale di conferma
//         */

//        [HttpPost]
//        [Authorize(Roles = "Admin")]
//        public IActionResult Importa(IFormFile fileExcel)//, int idMandatario ora uso la sessione
//        {
//            // Recupero IdMandatario dalla sessione (stabile, non dipende dal browser)
//            int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;//uso la sessione
//            if (idMandatario == 0)
//            {
//                TempData["Errore"] = "Si è persa la Sessione del  Mandatario .Ritorna alla Pagina Iniziale";
//                return View();
//            }

//            if (fileExcel == null || fileExcel.Length == 0)
//            {
//                TempData["Errore"] = "Seleziona un file Excel valido.";
//                return View();
//            }


//            // 0. Salvo il file in una cartella temporanea
//            var tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tempDeAngeli");
//            Directory.CreateDirectory(tempFolder);

//            var tempFileName = $"DEANGELI_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(fileExcel.FileName)}";
//            var tempFilePath = Path.Combine(tempFolder, tempFileName);

//            using (var stream = new FileStream(tempFilePath, FileMode.Create))
//            {
//                fileExcel.CopyTo(stream);
//            }


//            // 1. BACKUP FISICO DEL FILE DEANGELI (con gestione errori elegante)
//            try
//            {
//                _fileBackupService.BackupDeAngeliAsync(tempFilePath).Wait();
//            }
//            catch (Exception ex)
//            {
//                // Non blocchiamo l'importazione DEANGELI
//                // Registriamo l'errore in TempData (o log)
//                TempData["BackupWarning"] = "Backup non eseguito: " + ex.Message;

//                // Se hai un logger, puoi usare:
//                // _logger.LogError(ex, "Errore durante il backup del file DEANGELI.");
//            }



//            // 2. Leggo l’Excel → List<RigaExcelDeAngeli>
//            int firstRow = _config.GetValue<int>("ImportazioneDeAngeli:FirstRow");

//            var righe = ExcelHelper.LeggiExcelDeAngeli(fileExcel, firstRow);

//            // 3. Converto ogni riga in ImportDeAngeli
//            //Select = trasforma ogni elemento della collezione;
//            //r => Converti(r) = funzione lambda= Per ogni elemento r in righe, applica il metodo Converti(r);
//            var righeImportDeAngeli = righe.Select(r => Converti(r)).ToList();

//            // 4. Creo FileImportato
//            string utente = User.Identity?.Name ?? "Sistema";

//            int anno = int.Parse(Request.Form["Anno"]);
//            int mese = int.Parse(Request.Form["Mese"]);

//            var file = new FileImportato
//            {
//                IdMandatario = idMandatario,//uso la sessione
//                NomeFile = fileExcel.FileName,
//                DataImportazione = DateTime.Now,
//                Utente = utente,
//                Anno = anno,
//                Mese = mese

//            };

//            // 5. Avvio importazione ImportazioneDeAngeliService

//            //gestione del tipopagamento di default del mandatario tramite sessione, così non devo passarlo come parametro da form o URL
//            int IdTipoPagamento = HttpContext.Session.GetInt32("TipoPagamentoMandatario") ?? 0;
//            int idFile = _ImportazioneDeAngeliService.ImportaFile(file, righeImportDeAngeli, utente, IdTipoPagamento);

//            // 6. Mostra modale di conferma
//            TempData["ImportSuccess"] = true;
//            TempData["IdFileImportato"] = idFile;
//            // 7. Pulizia del file temporaneo (non blocca l'importazione)
//            try
//            {
//                if (System.IO.File.Exists(tempFilePath))
//                    System.IO.File.Delete(tempFilePath);
//            }
//            catch
//            {
//                // Non blocchiamo nulla se la cancellazione fallisce
//            }
//            //return RedirectToAction("Importa", "ProspettiDeAngeli", new { area = "Admin" });
//            //Così la pagina Importa si ricarica con il Mandatario corretto
//            return RedirectToAction("Importa", "ProspettiDeAngeli", new { area = "Admin", idMandatario = file.IdMandatario });

//        }


//        // ============================================================
//        // 4) CONVERSIONE RIGA EXCEL DEANGELI → ImportDeAngeli
//        // ============================================================
//        /*
//         * Questo metodo converte una riga Excel DEANGELI
//         * nel modello ImportDeAngeli usato dalla pipeline.
//         * 
//         * È specifico per DEANGELI, quindi sta nel controller DEANGELI.
//         */

//        private ImportDeAngeli Converti(RigaExcelDeAngeli r)
//        {
//            var i = new ImportDeAngeli();

//            i.IdCalcoloBase = r.IdCalcoloBase;
//            i.OrdineDDT = r.OrdineDDT;
//            i.Cliente = r.Cliente;
//            i.Kg = r.Kg;
//            i.QuantitaOrdine = r.QuantitaOrdine;
//            i.CostoLavorazione = r.CostoLavorazione;
//            i.Materiale = r.Materiale;
//            i.TipologiaMateriale = r.TipologiaMateriale;
//            i.Prezzo = r.Prezzo;
//            i.PrezzoConLavorazione = r.PrezzoConLavorazione;
//            i.PrezzoVendita = r.PrezzoVendita;
//            i.PercentualeProvvigioneVariabile = r.PercentualeProvvigioneVariabile;
//            i.ValoreProvvigioneVariabile = r.ValoreProvvigioneVariabile;
//            i.ValoreProvvigioneFissa1 = r.ValoreProvvigioneFissa1;
//            i.ValoreProvvigioneFissa2 = r.ValoreProvvigioneFissa2;
//            i.ValoreProvvigioneFissa3 = r.ValoreProvvigioneFissa3;
//            i.TotaleFissoProvvigione = r.TotaleFissoProvvigione;
//            i.TotaleVariabileProvvigione = r.TotaleVariabileProvvigione;
//            i.TotaleProvvigioneDaFatturare = r.TotaleProvvigioneDaFatturare;
//            i.Differenza = r.Differenza;
//            i.FlagIndicatoreMaggiorazione = r.FlagIndicatoreMaggiorazione?.ToLower() == "si" || r.FlagIndicatoreMaggiorazione?.ToLower() == "sì";
//            i.DataConsegnaIpotetica = r.DataConsegnaIpotetica;
//            i.Scadenza = r.Scadenza;
//            // Gestione flag booleano dalla stringa Excel ("si"/"no")
//            i.Fatturare = r.Fatturare?.ToLower() == "si" || r.Fatturare?.ToLower() == "sì";

//            // Campi tecnici per tracciamento
//            //i.IdMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;
//            //i.IsProcessed = false;
//            //i.DataInserimento = DateTime.Now;
//            //i.Utente = User.Identity?.Name ?? "Sistema"

//            return i;
//        }
//    }
//}

